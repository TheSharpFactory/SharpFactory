#region Usings
using SF.Common.Logging;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace SF.App.Services.Updates;

/// <summary>
/// Installs a release over the running app: downloads its package next to the app, checks it against the release's
/// SHA256SUMS.txt, unpacks it, then swaps the files. The running program cannot be overwritten (Windows), but it can
/// be renamed, so every file the package replaces is first renamed to "*.sf-old"; the new version deletes those when
/// it starts. If a step fails, the files already swapped are put back and the app is unchanged.
/// </summary>
internal sealed class UpdateInstaller
{
    public const string OldSuffix = ".sf-old";
    public const string StagingFolderName = ".sharpfactory-update";

    /// <summary>
    /// Held open, unshared, in the download folder while an update runs: another copy of the app (its clean-up when
    /// it starts, or its own update) leaves that folder alone.
    /// </summary>
    public const string LockFileName = ".lock";

    private static readonly TimeSpan _checksumsTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _downloadTimeout = TimeSpan.FromMinutes(30);

    private readonly HttpClient _http;
    private readonly ReleaseInfo _release;
    private readonly InstallLocation _location;
    private readonly Version _currentVersion;
    private readonly IProgress<UpdateProgress> _progress;
    private readonly UpdateCancellation _cancellation;
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// <paramref name="progress"/> and <paramref name="cancellation"/> may be null (no progress, no Cancel).
    /// </summary>
    public UpdateInstaller(HttpClient http, ReleaseInfo release, InstallLocation location, Version currentVersion, IProgress<UpdateProgress> progress, UpdateCancellation cancellation)
    {
        _http = http;
        _release = release;
        _location = location;
        _currentVersion = currentVersion;
        _progress = progress;
        _cancellation = cancellation;
        _cancellationToken = cancellation != null ? cancellation.Token : CancellationToken.None;
    }

    /// <summary>
    /// Runs every step; throws UpdateException with a message for the user when one fails.
    /// </summary>
    public async Task RunAsync()
    {
        var staging = Path.Combine(_location.Folder, StagingFolderName);
        //outside the try: when another copy is updating, its download folder must not be deleted here
        var stagingLock = CreateStaging(staging);
        try
        {
            var package = Path.Combine(staging, _release.PackageName);
            await DownloadAsync(package);
            var unpacked = Path.Combine(staging, "package");
            Unpack(package, unpacked);
            CheckPackage(unpacked);

            //the point of no return: Cancel works until here, never after (the swap is quick, and stopping it halfway
            //would only mean putting files back)
            if(_cancellation != null && !_cancellation.TryBeginReplacing())
                throw new OperationCanceledException(_cancellationToken);

            //from here on Cancel no longer works
            _progress?.Report(new UpdateProgress { Stage = UpdateStage.Replacing });
            Swap(unpacked);
        }
        finally
        {
            stagingLock.Dispose();
            DeleteQuietly(staging);
        }
    }

    /// <summary>
    /// Downloads SHA256SUMS.txt and the package, and checks the package's SHA-256. A lost connection or the time limit,
    /// while asking or while reading, is an UpdateException; Cancel is an OperationCanceledException.
    /// </summary>
    private async Task DownloadAsync(string package)
    {
        try
        {
            var expectedHash = await DownloadExpectedHashAsync();
            var actualHash = await DownloadPackageAsync(package);
            if(!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
                throw new UpdateException($"The download of version {_release.Version} is damaged (its SHA-256 does not match the release's). Nothing was changed.");
        }
        catch(HttpRequestException ex)
        {
            throw new UpdateException($"The download failed ({ex.Message}). Nothing was changed.", ex);
        }
        catch(IOException ex)
        {
            //a connection lost while reading (HttpIOException), or the download could not be saved
            throw new UpdateException($"The download failed ({ex.Message}). Nothing was changed.", ex);
        }
        catch(OperationCanceledException ex) when(!_cancellationToken.IsCancellationRequested)
        {
            throw new UpdateException("The download took too long. Nothing was changed.", ex);
        }
    }

    /// <summary>
    /// A fresh download folder, and its lock (held until the update ends).
    /// </summary>
    private FileStream CreateStaging(string staging)
    {
        if(IsStagingInUse(staging))
            throw new UpdateException("Another copy of the app is updating this folder right now. Wait for it to finish.");

        try
        {
            DeleteEntry(staging);
            Directory.CreateDirectory(staging);
            return new FileStream(Path.Combine(staging, LockFileName), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }
        catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new UpdateException($"The app cannot write to {_location.Folder}, so it cannot update itself there.", ex);
        }
    }

    /// <summary>
    /// True while an update (of this copy or another) holds the download folder's lock. On Linux and macOS .NET
    /// takes an advisory lock for FileShare.None, so this works there too.
    /// </summary>
    public static bool IsStagingInUse(string staging)
    {
        var lockFile = Path.Combine(staging, LockFileName);
        if(!File.Exists(lockFile))
            return false;

        try
        {
            using(new FileStream(lockFile, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                return false;
            }
        }
        catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>
    /// The package's SHA-256 from SHA256SUMS.txt (lines of "hash  file name", as sha256sum writes them).
    /// </summary>
    private async Task<string> DownloadExpectedHashAsync()
    {
        using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken))
        {
            timeout.CancelAfter(_checksumsTimeout);
            using(var request = UpdateChecker.CreateRequest(_release.ChecksumsUri, _currentVersion))
            {
                using(var response = await SendAsync(request, timeout.Token))
                {
                    var text = await response.Content.ReadAsStringAsync(timeout.Token);
                    var hash = FindHash(text, _release.PackageName);
                    if(hash == null)
                        throw new UpdateException($"The release has no checksum for {_release.PackageName}. Nothing was changed.");

                    return hash;
                }
            }
        }
    }

    public static string FindHash(string checksums, string fileName)
    {
        if(checksums == null)
            return null;

        foreach(var line in checksums.Split('\n'))
        {
            var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if(parts.Length != 2)
                continue;

            //"*name" is sha256sum's binary mode
            var name = parts[1].TrimStart('*');
            if(string.Equals(name, fileName, StringComparison.Ordinal) && parts[0].Length == 64)
                return parts[0];
        }

        return null;
    }

    /// <summary>
    /// Saves the package and returns its SHA-256 (hex), reporting the percentage done.
    /// </summary>
    private async Task<string> DownloadPackageAsync(string package)
    {
        using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken))
        {
            timeout.CancelAfter(_downloadTimeout);
            using(var request = UpdateChecker.CreateRequest(_release.PackageUri, _currentVersion))
            {
                using(var response = await SendAsync(request, timeout.Token))
                {
                    var length = response.Content.Headers.ContentLength;
                    using(var source = await response.Content.ReadAsStreamAsync(timeout.Token))
                    {
                        using(var target = new FileStream(package, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            using(var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                            {
                                var buffer = new byte[81920];
                                var total = 0L;
                                var lastPercent = -1;
                                var read = 0;
                                while((read = await source.ReadAsync(buffer, timeout.Token)) > 0)
                                {
                                    await target.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
                                    hash.AppendData(buffer, 0, read);
                                    total += read;

                                    var percent = length > 0 ? (int)(total * 100 / length.Value) : -1;
                                    if(percent != lastPercent)
                                    {
                                        lastPercent = percent;
                                        _progress?.Report(new UpdateProgress { Stage = UpdateStage.Downloading, Percent = percent });
                                    }
                                }

                                return Convert.ToHexString(hash.GetHashAndReset());
                            }
                        }
                    }
                }
            }
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if(!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            response.Dispose();
            throw new UpdateException($"The download failed: {request.RequestUri} answered {status}. Nothing was changed.");
        }

        return response;
    }

    /// <summary>
    /// A .zip (Windows, macOS) or .tar.gz (Linux, which keeps the programs executable).
    /// </summary>
    private static void Unpack(string package, string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            if(package.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                using(var file = File.OpenRead(package))
                {
                    using(var gzip = new GZipStream(file, CompressionMode.Decompress))
                    {
                        TarFile.ExtractToDirectory(gzip, folder, false);
                    }
                }
            }
            else
            {
                ZipFile.ExtractToDirectory(package, folder);
            }
        }
        catch(Exception ex) when(ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException)
        {
            throw new UpdateException($"The download could not be unpacked ({ex.Message}). Nothing was changed.", ex);
        }
    }

    /// <summary>
    /// The package must have the program at the same place as the running one; on Linux and macOS the programs are
    /// made executable (a .zip does not always keep that).
    /// </summary>
    private void CheckPackage(string unpacked)
    {
        var program = Path.Combine(unpacked, _location.RelativeExecutable);
        if(!File.Exists(program))
            throw new UpdateException($"The download has no {_location.RelativeExecutable}. Nothing was changed.");

        if(OperatingSystem.IsWindows())
            return;

        var cli = Path.Combine(Path.GetDirectoryName(program), "sharpfactory");
        MakeExecutable(program);
        if(File.Exists(cli))
            MakeExecutable(cli);
    }

    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    private static void MakeExecutable(string file)
    {
        var mode = File.GetUnixFileMode(file);
        File.SetUnixFileMode(file, mode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
    }

    /// <summary>
    /// Renames each file or folder the package replaces to "*.sf-old" and moves the new one in. On a failure the
    /// moves done so far are undone in reverse.
    /// </summary>
    private void Swap(string unpacked)
    {
        var done = new List<SwapStep>();
        try
        {
            //the list is taken first: the loop moves the entries out of the folder
            foreach(var entry in Directory.GetFileSystemEntries(unpacked))
            {
                var name = Path.GetFileName(entry);
                var step = new SwapStep { NewPath = entry, TargetPath = Path.Combine(_location.Folder, name) };
                step.OldPath = step.TargetPath + OldSuffix;

                DeleteEntry(step.OldPath);
                if(DoesEntryExist(step.TargetPath))
                {
                    MoveEntry(step.TargetPath, step.OldPath);
                    step.MovedTarget = true;
                }

                done.Add(step);
                MoveEntry(step.NewPath, step.TargetPath);
                step.MovedNew = true;
            }
        }
        catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
        {
            if(Undo(done))
                throw new UpdateException($"The files in {_location.Folder} could not be replaced ({ex.Message}). The app was left as it was.", ex);

            throw new UpdateException($"The files in {_location.Folder} could not be replaced ({ex.Message}), and some of the old ones could not be put back. Download version {_release.Version} from its release page and unpack it there again.", ex);
        }
    }

    /// <summary>
    /// Puts back what the swap moved, last first. Returns false when something could not be put back.
    /// </summary>
    private static bool Undo(List<SwapStep> done)
    {
        var restored = true;
        for(var i = done.Count - 1; i >= 0; i--)
        {
            var step = done[i];
            try
            {
                if(step.MovedNew)
                    MoveEntry(step.TargetPath, step.NewPath);

                if(step.MovedTarget)
                    MoveEntry(step.OldPath, step.TargetPath);
            }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
            {
                restored = false;
                LogManager.WriteErrorLog(ex, "Unable to put back a file the update replaced", step.TargetPath);
            }
        }

        return restored;
    }

    /// <summary>
    /// Deletes what an earlier update left in the app's folder: the "*.sf-old" files and folders and the download
    /// folder. A program that has not exited yet cannot be deleted (Windows): tries again a few times, then leaves
    /// it for the next start.
    /// </summary>
    public static async Task CleanUpAsync(string folder)
    {
        if(string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        for(var attempt = 0; attempt < 10; attempt++)
        {
            if(TryCleanUp(folder))
                return;

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public static bool TryCleanUp(string folder)
    {
        var clean = true;
        try
        {
            //the list is taken first: the loop deletes entries from the folder
            foreach(var entry in Directory.GetFileSystemEntries(folder))
            {
                var name = Path.GetFileName(entry);
                var isStaging = string.Equals(name, StagingFolderName, StringComparison.Ordinal);
                if(!name.EndsWith(OldSuffix, StringComparison.Ordinal) && !isStaging)
                    continue;

                //another copy of the app is updating: its download is not left over
                if(isStaging && IsStagingInUse(entry))
                    continue;

                try
                {
                    DeleteEntry(entry);
                }
                catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
                {
                    clean = false;
                }
            }
        }
        catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
        {
            LogManager.WriteErrorLog(ex, "Unable to clean up after an update", folder);
        }

        return clean;
    }

    private static bool DoesEntryExist(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    private static void MoveEntry(string from, string to)
    {
        if(Directory.Exists(from))
            Directory.Move(from, to);
        else
            File.Move(from, to);
    }

    private static void DeleteEntry(string path)
    {
        if(Directory.Exists(path))
            Directory.Delete(path, true);
        else if(File.Exists(path))
            File.Delete(path);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            DeleteEntry(path);
        }
        catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
        {
            LogManager.WriteErrorLog(ex, "Unable to delete the update's download folder", path);
        }
    }

    /// <summary>
    /// One file or folder of the package: where it is, what it replaces, and how far the swap got.
    /// </summary>
    private sealed class SwapStep
    {
        public string NewPath { get; set; }
        public string TargetPath { get; set; }
        public string OldPath { get; set; }
        public bool MovedTarget { get; set; }
        public bool MovedNew { get; set; }
    }
}

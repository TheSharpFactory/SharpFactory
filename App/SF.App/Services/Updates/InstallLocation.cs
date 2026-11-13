#region Usings
using System;
using System.Diagnostics;
using System.IO;
#endregion

namespace SF.App.Services.Updates;

/// <summary>
/// Where the running app was unpacked, and whether an update can replace it there. A release download is
/// TheSharpFactory(.exe), sharpfactory(.exe) and the license files in one folder; on macOS it is TheSharpFactory.app.
/// </summary>
internal sealed class InstallLocation
{
    public const string AppName = "TheSharpFactory";
    public const string BundleName = "TheSharpFactory.app";

    /// <summary>
    /// The folder the download's files and folders go to (on macOS the folder that holds the .app).
    /// </summary>
    public string Folder { get; set; }

    /// <summary>
    /// The running program; the same path in the new download is the program started after the update.
    /// </summary>
    public string Executable { get; set; }

    /// <summary>
    /// The .app bundle on macOS (started with "open"); null elsewhere.
    /// </summary>
    public string Bundle { get; set; }

    /// <summary>
    /// Why the app cannot update itself here, for the user; null when it can.
    /// </summary>
    public string Problem { get; set; }

    /// <summary>
    /// The running program's path relative to <see cref="Folder"/>, which is where it is in the download too.
    /// </summary>
    public string RelativeExecutable
    {
        get
        {
            return Path.GetRelativePath(Folder, Executable);
        }
    }

    /// <summary>
    /// Finds the release layout around the running program. A build run from the source (TheSharpFactory.dll next
    /// to the program) and a macOS app run from its temporary read-only copy cannot be updated in place.
    /// </summary>
    public static InstallLocation Find(string processPath, bool isMacOS)
    {
        if(string.IsNullOrWhiteSpace(processPath))
            return new InstallLocation { Problem = "The app could not find where it runs from." };

        var folder = Path.GetDirectoryName(processPath);
        var fileName = Path.GetFileName(processPath);
        if(isMacOS)
            return FindBundle(processPath, folder, fileName);

        var expected = OperatingSystem.IsWindows() ? AppName + ".exe" : AppName;
        if(!string.Equals(fileName, expected, StringComparison.OrdinalIgnoreCase) || File.Exists(Path.Combine(folder, AppName + ".dll")))
            return new InstallLocation { Executable = processPath, Problem = "This copy of the app is not a release download, so it cannot update itself." };

        return new InstallLocation { Folder = folder, Executable = processPath };
    }

    private static InstallLocation FindBundle(string processPath, string folder, string fileName)
    {
        //TheSharpFactory.app/Contents/MacOS/TheSharpFactory
        var contents = Path.GetDirectoryName(folder);
        var bundle = contents == null ? null : Path.GetDirectoryName(contents);
        var isReleaseLayout = string.Equals(fileName, AppName, StringComparison.Ordinal)
            && string.Equals(Path.GetFileName(folder), "MacOS", StringComparison.Ordinal)
            && string.Equals(Path.GetFileName(contents), "Contents", StringComparison.Ordinal)
            && string.Equals(Path.GetFileName(bundle), BundleName, StringComparison.Ordinal);

        if(!isReleaseLayout)
            return new InstallLocation { Executable = processPath, Problem = "This copy of the app is not a release download, so it cannot update itself." };

        //macOS runs an app opened straight from a download from a temporary read-only copy
        if(processPath.Contains("/AppTranslocation/", StringComparison.Ordinal))
        {
            return new InstallLocation
            {
                Executable = processPath,
                Problem = "macOS is running the app from a temporary copy. Move TheSharpFactory.app to Applications (or any folder), open it from there and update again.",
            };
        }

        return new InstallLocation { Folder = Path.GetDirectoryName(bundle), Executable = processPath, Bundle = bundle };
    }

    /// <summary>
    /// Starts the app from this location: the program, or the bundle through "open" on macOS. The model file that
    /// was open, if any, opens again.
    /// </summary>
    public ProcessStartInfo CreateStartInfo(string modelFile)
    {
        var startInfo = Bundle != null ? new ProcessStartInfo("open") : new ProcessStartInfo(Executable);
        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = Folder;
        if(Bundle != null)
        {
            startInfo.ArgumentList.Add("-n");
            startInfo.ArgumentList.Add(Bundle);
        }

        if(!string.IsNullOrWhiteSpace(modelFile))
        {
            if(Bundle != null)
                startInfo.ArgumentList.Add("--args");

            startInfo.ArgumentList.Add(modelFile);
        }

        return startInfo;
    }
}

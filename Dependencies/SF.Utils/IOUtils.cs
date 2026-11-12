#region Usings
using System;
using System.IO;
using System.Threading;
#endregion

namespace SF.Utils;

public static class IOUtils
{
    private const string _logFileName = "log";
    private static readonly Lock _logSyncRoot = new Lock();

    /// <summary>
    /// A schema-reader log rolls over when it passes this size: log.txt becomes log.1.txt, and so on.
    /// </summary>
    public const long LogFileMaxBytes = 150 * 1024;

    /// <summary>
    /// How many log files are kept per model and source (log.txt and up to log.4.txt); the oldest is deleted.
    /// </summary>
    public const int LogFilesKept = 5;

    /// <summary>
    /// Where WriteLog puts the schema reader's per-model error logs. The app sets it to its per-user log folder;
    /// the default (the app's own folder) also works in single-file apps and test hosts.
    /// </summary>
    public static string LogFolder { get; set; } = AppContext.BaseDirectory;

    /// <summary>
    /// When false, CreateFolder and CreateSubFolder only compute paths and create nothing (Sync preview).
    /// </summary>
    public static bool CreateFolders { get; set; } = true;

    /// <summary>
    /// Generated files always use CRLF, on every operating system. The generator ends lines with the
    /// operating system's new line, so on macOS and Linux this turns its LF into CRLF; on Windows nothing changes.
    /// </summary>
    public static string ToCrlf(string content)
    {
        if(string.IsNullOrEmpty(content))
            return content;

        return content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
    }

    public static bool IsFileContentDifferent(string fileName, string newContent)
    {
        if(!File.Exists(fileName))
            return true;

        var content = File.ReadAllText(fileName);
        if(content.Length != newContent.Length)
            return true;

        return string.CompareOrdinal(content, newContent) != 0;
    }
    public static void CreateFolder(string path)
    {
        if(CreateFolders && !Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
    public static string CreateSubFolder(string path, string subFolder)
    {
        if(string.IsNullOrWhiteSpace(subFolder))
            return path;

        //some generators name nested folders with a backslash (Query\Builders): make it a real folder on every OS
        var result = Path.Combine(path, subFolder.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
        CreateFolder(result);
        return result;
    }
    /// <summary>
    /// Appends an entry, with its date and time, to the model's log for <paramref name="source"/>:
    /// LogFolder\&lt;model&gt;Logs\&lt;source&gt;\log.txt. Past LogFileMaxBytes the file rolls over (log.1.txt is the newest
    /// older file); LogFilesKept files are kept.
    /// </summary>
    public static void WriteLog(string modelName, string source, string content)
    {
        var folder = Path.Combine(LogFolder, modelName + "Logs", source);
        var file = Path.Combine(folder, _logFileName + ".txt");
        var entry = $"**** {DateTime.Now:yyyy-MM-dd HH:mm:ss} ****{Environment.NewLine}{content}{Environment.NewLine}";
        lock(_logSyncRoot)
        {
            Directory.CreateDirectory(folder);
            if(File.Exists(file) && new FileInfo(file).Length >= LogFileMaxBytes)
                RollOver(folder);

            File.AppendAllText(file, entry);
        }
    }

    /// <summary>
    /// log.3.txt becomes log.4.txt, ... log.txt becomes log.1.txt; the oldest beyond LogFilesKept is deleted.
    /// </summary>
    private static void RollOver(string folder)
    {
        DeleteIfExists(RolledLogFile(folder, LogFilesKept - 1));
        for(var i = LogFilesKept - 2; i >= 0; i--)
        {
            var from = RolledLogFile(folder, i);
            if(File.Exists(from))
                File.Move(from, RolledLogFile(folder, i + 1));
        }
    }

    private static string RolledLogFile(string folder, int index)
    {
        return Path.Combine(folder, index == 0 ? _logFileName + ".txt" : $"{_logFileName}.{index}.txt");
    }
    /// <summary>
    /// Writes the file so a failure never leaves it half written: the content goes to a temporary file in the same
    /// folder, which then takes its place. UTF-8 without a BOM, as File.WriteAllText.
    /// </summary>
    public static void SaveFile(string file, string content)
    {
        var fullPath = Path.GetFullPath(file);
        var tempFile = Path.Combine(Path.GetDirectoryName(fullPath), "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(tempFile, content);
        }
        catch
        {
            DeleteIfExists(tempFile);
            throw;
        }

        if(File.Exists(fullPath))
            ReplaceWith(tempFile, fullPath);
        else
            MoveInPlace(tempFile, fullPath);
    }

    /// <summary>
    /// Puts the temporary file where no file is yet.
    /// </summary>
    private static void MoveInPlace(string tempFile, string fullPath)
    {
        try
        {
            File.Move(tempFile, fullPath);
        }
        catch
        {
            //the file appeared in the meantime: replace it instead
            if(File.Exists(fullPath) && File.Exists(tempFile))
            {
                ReplaceWith(tempFile, fullPath);
                return;
            }

            DeleteIfExists(tempFile);
            throw;
        }
    }

    /// <summary>
    /// Replaces the file with the temporary file. When Windows cannot replace it, the old file is never lost: if it is
    /// already gone the temporary file (then the only copy) is moved into its place; if it is still there it is written
    /// in place, as before (another program holds it open without delete sharing, it is read-only, or the file system
    /// cannot replace files).
    /// </summary>
    private static void ReplaceWith(string tempFile, string fullPath)
    {
        try
        {
            File.Replace(tempFile, fullPath, null);
            return;
        }
        catch
        {
            //handled below
        }

        //ReplaceFile can fail after the old file is gone (ERROR_UNABLE_TO_MOVE_REPLACEMENT)
        if(!File.Exists(fullPath))
        {
            try
            {
                File.Move(tempFile, fullPath);
            }
            catch(Exception ex)
            {
                throw new IOException($"{fullPath} could not be saved. Its new content is in {tempFile}.", ex);
            }

            return;
        }

        try
        {
            File.Copy(tempFile, fullPath, true);
        }
        finally
        {
            DeleteIfExists(tempFile);
        }
    }

    private static void DeleteIfExists(string file)
    {
        if(File.Exists(file))
            File.Delete(file);
    }
    public static bool FileExists(string file)
    {
        return File.Exists(file);
    }
    public static string FileReadContent(string file)
    {
        return File.ReadAllText(file);
    }
}

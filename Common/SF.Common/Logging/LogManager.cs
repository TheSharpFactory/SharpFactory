#region Usings
using System;
using System.IO;
using System.Text;
using System.Threading;
#endregion

namespace SF.Common.Logging;

public static class LogManager
{
    private static bool _enabled = false;
    public static string LogFolder { get; private set; }
    private static readonly Lock _eventLogSyncRoot = new Lock();

    public static void Enable(string logFolder)
    {
        if(_enabled)
            return;

        if(!Directory.Exists(logFolder))
            Directory.CreateDirectory(logFolder);

        LogFolder = logFolder;
        _enabled = true;
    }
    public static void WriteErrorLog(object exObj, string title, string optionalInfo = "")
    {
        if(!_enabled)
            return;

        var b = new StringBuilder(1000);

        b.AppendLine(title);
        if(!string.IsNullOrWhiteSpace(optionalInfo))
            b.AppendLine(optionalInfo);

        if(exObj != null)
        {
            var ex = exObj as Exception;
            if(ex != null)
            {
                ComposeExceptionDescription(b, ex, string.Empty);
            }
        }

        WriteLog(b.ToString(), LogEntryType.Error);
    }
    public static void WriteLog(string msg, LogEntryType type)
    {
        if(!_enabled)
            return;

        var date = DateTime.Now;
        var dateFolder = DateTime.Now.ToString("yyyy_MMM_dd");
        var directory = Path.Combine(LogFolder, type.ToString(), dateFolder);
        //files are created by minute so that they never over grow.
        //files have the thread id to reduce file locking
        var fileName = Path.Combine(directory, date.ToString("hh-mmtt_") + "T" + Thread.CurrentThread.ManagedThreadId.ToString() + ".txt");
        var b = new StringBuilder(msg.Length + 200);
        b.AppendLine();
        b.AppendLine("****************************************************");
        b.AppendLine("Date: " + date.ToLongDateString());
        b.AppendLine("Time: " + date.ToLongTimeString());
        b.AppendLine(msg);
        b.AppendLine("****************************************************");
        b.AppendLine();

        try
        {
            lock(_eventLogSyncRoot)
            {
                //if there was an error just add the log to a text file.
                if(!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.AppendAllText(fileName, b.ToString());
            }
        }
        catch //(Exception ex)
        {
        }
    }
    private static void ComposeExceptionDescription(StringBuilder b, Exception ex, string indent)
    {
        b.AppendLine(indent + "Exception Type: " + ex.GetType().Name);
        b.AppendLine();
        b.AppendLine(indent + "Message: " + ex.Message);
        b.AppendLine();

        if(ex.InnerException != null)
        {
            var childIndent = indent + "    ";
            b.AppendLine(childIndent + "    " + "Inner Exception:");
            ComposeExceptionDescription(b, ex.InnerException, childIndent);
            b.AppendLine(childIndent + "    " + "End Inner Exception:");
            b.AppendLine();
        }

        b.AppendLine(indent + "StackTrace: " + ex.StackTrace);
        b.AppendLine();
        b.AppendLine(indent + "TargetSite: " + ex.TargetSite);
        b.AppendLine();
        b.AppendLine(indent + "Source: " + ex.Source);
    }
}

#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
#endregion

namespace SF.Engine.Targets;

/// <summary>
/// A solution an IDE seems to have open: its path when the command line names it, otherwise only its name
/// (from the window title), to be matched against known solutions.
/// </summary>
public sealed class OpenSolutionHint
{
    public string IdeName { get; set; }
    public string SolutionPath { get; set; }
    public string SolutionName { get; set; }
}

/// <summary>
/// Finds the solutions open in Visual Studio, Rider and VS Code without connecting to them: from the
/// processes' command lines and, on Windows, their window titles. Best effort: it never throws.
/// </summary>
public static class RunningIdeScanner
{
    public static List<OpenSolutionHint> Scan()
    {
        var result = new List<OpenSolutionHint>();
        try
        {
            if(OperatingSystem.IsWindows())
                ScanWindows(result);
            else
                ScanUnix(result);
        }
        catch
        {
            //a missing permission or tool only means fewer suggestions
        }

        return result;
    }

    [SupportedOSPlatform("windows")]
    private static void ScanWindows(List<OpenSolutionHint> result)
    {
        var query = "SELECT ProcessId, Name, CommandLine FROM Win32_Process WHERE Name = 'devenv.exe' OR Name = 'rider64.exe' OR Name = 'rider.exe' OR Name = 'Code.exe'";
        using(var searcher = new ManagementObjectSearcher(query))
        {
            foreach(var item in searcher.Get())
            {
                var name = Convert.ToString(item["Name"]);
                var commandLine = Convert.ToString(item["CommandLine"]);
                var processId = Convert.ToInt32(item["ProcessId"]);
                var ideName = GetIdeName(name);
                var found = AddSolutionsFromCommandLine(result, ideName, commandLine);

                if(!found)
                    AddSolutionFromWindowTitle(result, ideName, processId);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void AddSolutionFromWindowTitle(List<OpenSolutionHint> result, string ideName, int processId)
    {
        try
        {
            using(var process = Process.GetProcessById(processId))
            {
                var title = process.MainWindowTitle;
                if(string.IsNullOrWhiteSpace(title))
                    return;

                //"Solution - File.cs - Microsoft Visual Studio", "Solution – File.cs" (Rider), "File - Folder - Visual Studio Code"
                var parts = title.Split(new[] { " - ", " – " }, StringSplitOptions.RemoveEmptyEntries);
                var solutionName = string.Equals(ideName, "VS Code", StringComparison.Ordinal) && parts.Length > 1 ? parts[parts.Length - 2] : parts[0];
                result.Add(new OpenSolutionHint { IdeName = ideName, SolutionName = solutionName.Trim() });
            }
        }
        catch
        {
            //the process may have exited
        }
    }

    private static void ScanUnix(List<OpenSolutionHint> result)
    {
        //ww: never cut long command lines (macOS ps otherwise trims them to a fixed width)
        var start = new ProcessStartInfo("ps", "-axww -o args=")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        using(var ps = Process.Start(start))
        {
            var output = ps.StandardOutput.ReadToEnd();
            ps.WaitForExit();

            foreach(var line in output.Split('\n'))
            {
                var ideName = GetIdeNameFromCommandLine(line);
                if(ideName != null)
                    AddSolutionsFromCommandLine(result, ideName, line);
            }
        }
    }

    /// <summary>
    /// Adds the .sln/.slnx paths in a command line, and the solutions in folders it opens (VS Code, Rider).
    /// </summary>
    private static bool AddSolutionsFromCommandLine(List<OpenSolutionHint> result, string ideName, string commandLine)
    {
        if(string.IsNullOrWhiteSpace(commandLine))
            return false;

        var found = false;
        foreach(var token in SplitCommandLine(commandLine))
        {
            if(IsSolutionFile(token) && File.Exists(token))
            {
                result.Add(new OpenSolutionHint { IdeName = ideName, SolutionPath = Path.GetFullPath(token) });
                found = true;
            }
            else if(Path.IsPathRooted(token) && Directory.Exists(token))
            {
                foreach(var solution in FindSolutionsInFolder(token))
                {
                    result.Add(new OpenSolutionHint { IdeName = ideName, SolutionPath = solution });
                    found = true;
                }
            }
        }

        return found;
    }

    public static List<string> FindSolutionsInFolder(string folder)
    {
        var result = new List<string>();
        try
        {
            foreach(var file in Directory.EnumerateFiles(folder, "*.sln", SearchOption.TopDirectoryOnly))
                result.Add(Path.GetFullPath(file));

            foreach(var file in Directory.EnumerateFiles(folder, "*.slnx", SearchOption.TopDirectoryOnly))
                result.Add(Path.GetFullPath(file));
        }
        catch
        {
            //no access to the folder
        }

        return result;
    }

    public static bool IsSolutionFile(string path)
    {
        return path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Splits a command line into arguments, honoring double quotes.
    /// </summary>
    public static List<string> SplitCommandLine(string commandLine)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach(var c in commandLine)
        {
            if(c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if(char.IsWhiteSpace(c) && !inQuotes)
            {
                if(current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if(current.Length > 0)
            result.Add(current.ToString());

        return result;
    }

    private static string GetIdeName(string processName)
    {
        if(processName.StartsWith("devenv", StringComparison.OrdinalIgnoreCase))
            return "Visual Studio";

        if(processName.StartsWith("rider", StringComparison.OrdinalIgnoreCase))
            return "Rider";

        return "VS Code";
    }

    private static string GetIdeNameFromCommandLine(string commandLine)
    {
        var lower = commandLine.ToLowerInvariant();
        if(lower.Contains("rider", StringComparison.Ordinal))
            return "Rider";

        if(lower.Contains("visual studio code", StringComparison.Ordinal) || lower.Contains("/code", StringComparison.Ordinal) || lower.StartsWith("code ", StringComparison.Ordinal))
            return "VS Code";

        //only IDEs: a build or a sync that names a solution (dotnet build App.sln) has nothing open
        return null;
    }
}

#region Usings
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
#endregion

namespace SF.Engine.Targets;

/// <summary>
/// The solutions Visual Studio and Rider opened recently, read from their own settings files. An IDE's window
/// title only gives a solution's name; this list turns that name into a path. Best effort: it never throws.
/// </summary>
public static class RecentSolutionsReader
{
    private static readonly Regex _vsCodeContainers = new Regex("<collection name=\"CodeContainers\\.Offline\">\\s*<value name=\"value\">(?<json>[^<]*)</value>", RegexOptions.Compiled);
    private static readonly Regex _riderEntry = new Regex("(?:key|value)=\"(?<path>[^\"]*\\.slnx?)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Recent .sln/.slnx paths that still exist, Visual Studio's first (most recent first), then Rider's.
    /// </summary>
    public static List<string> Read()
    {
        var result = new List<string>();
        try
        {
            AddVisualStudio(result);
            AddRider(result);
        }
        catch
        {
            //unreadable settings only mean fewer suggestions
        }

        return result;
    }

    /// <summary>
    /// The solution paths in Visual Studio's ApplicationPrivateSettings.xml (the CodeContainers.Offline list).
    /// </summary>
    public static List<string> ParseVisualStudioSettings(string xml)
    {
        var result = new List<string>();
        var match = _vsCodeContainers.Match(xml);
        if(!match.Success)
            return result;

        var json = WebUtility.HtmlDecode(match.Groups["json"].Value);
        foreach(var item in JArray.Parse(json))
        {
            var path = (string)item.SelectToken("Value.LocalProperties.FullPath") ?? (string)item["Key"];
            if(!string.IsNullOrWhiteSpace(path) && RunningIdeScanner.IsSolutionFile(path))
                result.Add(path);
        }

        return result;
    }

    /// <summary>
    /// The solution paths in Rider's recentSolutions.xml or recentProjects.xml.
    /// </summary>
    public static List<string> ParseRiderSettings(string xml, string userHome)
    {
        var result = new List<string>();
        var matches = _riderEntry.Matches(xml);
        for(var i = 0; i < matches.Count; i++)
        {
            var path = WebUtility.HtmlDecode(matches[i].Groups["path"].Value).Replace("$USER_HOME$", userHome);
            result.Add(path.Replace('/', Path.DirectorySeparatorChar));
        }

        return result;
    }

    private static void AddVisualStudio(List<string> result)
    {
        if(!OperatingSystem.IsWindows())
            return;

        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "VisualStudio");
        if(!Directory.Exists(root))
            return;

        //newest Visual Studio first: 18.0_x before 17.0_x
        var folders = new List<string>(Directory.EnumerateDirectories(root));
        folders.Sort(StringComparer.OrdinalIgnoreCase);
        folders.Reverse();
        foreach(var folder in folders)
        {
            var file = Path.Combine(folder, "ApplicationPrivateSettings.xml");
            if(File.Exists(file))
                AddExisting(result, ParseVisualStudioSettings(File.ReadAllText(file)));
        }
    }

    private static void AddRider(List<string> result)
    {
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roots = new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JetBrains"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JetBrains"),
            Path.Combine(userHome, "Library", "Application Support", "JetBrains"),
        };

        foreach(var root in roots)
        {
            if(!Directory.Exists(root))
                continue;

            foreach(var folder in Directory.EnumerateDirectories(root, "Rider*"))
            {
                foreach(var name in new[] { "recentSolutions.xml", "recentProjects.xml" })
                {
                    var file = Path.Combine(folder, "options", name);
                    if(File.Exists(file))
                        AddExisting(result, ParseRiderSettings(File.ReadAllText(file), userHome));
                }
            }
        }
    }

    private static void AddExisting(List<string> result, List<string> paths)
    {
        foreach(var path in paths)
        {
            if(File.Exists(path) && !result.Contains(path))
                result.Add(path);
        }
    }
}

#region Usings
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#endregion

namespace SF.Engine.Sync;

/// <summary>
/// Reads and adds project references, and sets the target framework, in a project file's text, keeping the rest of
/// the file exactly as it is.
/// </summary>
public static class ProjectFileEditor
{
    private static readonly Regex _projectReference = new Regex("<ProjectReference\\s+Include\\s*=\\s*\"(?<path>[^\"]*)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _targetFramework = new Regex("<TargetFramework>\\s*(?<tfm>[^<\\s]*)\\s*</TargetFramework>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _targetFrameworks = new Regex("<TargetFrameworks>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    //"net10.0", "net11.0": not "net10.0-windows", not a property like "$(MyFramework)"
    private static readonly Regex _plainNetVersion = new Regex("^net[0-9]+\\.[0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// The project name (file name without extension) of each project reference, with its Include path.
    /// References inside XML comments do not count.
    /// </summary>
    public static List<ProjectReferenceInfo> GetProjectReferences(string projectFileContent)
    {
        var result = new List<ProjectReferenceInfo>();
        var comments = GetCommentRanges(projectFileContent);
        var matches = _projectReference.Matches(projectFileContent);
        for(var i = 0; i < matches.Count; i++)
        {
            if(IsInside(comments, matches[i].Index))
                continue;

            var group = matches[i].Groups["path"];
            result.Add(new ProjectReferenceInfo
            {
                Include = group.Value,
                Name = GetProjectName(group.Value),
                Index = group.Index,
            });
        }

        return result;
    }

    /// <summary>
    /// Makes the project reference to <paramref name="projectName"/> point to <paramref name="relativePath"/>:
    /// adds it when missing, fixes its path when it points somewhere else. Returns the new content.
    /// </summary>
    public static string SetProjectReference(string projectFileContent, string projectName, string relativePath, out bool changed)
    {
        changed = false;
        foreach(var reference in GetProjectReferences(projectFileContent))
        {
            if(!string.Equals(reference.Name, projectName, StringComparison.OrdinalIgnoreCase))
                continue;

            if(string.Equals(NormalizeSeparators(reference.Include), NormalizeSeparators(relativePath), StringComparison.OrdinalIgnoreCase))
                return projectFileContent;

            changed = true;
            return projectFileContent.Substring(0, reference.Index) + relativePath + projectFileContent.Substring(reference.Index + reference.Include.Length);
        }

        changed = true;
        return AddProjectReference(projectFileContent, relativePath);
    }

    /// <summary>
    /// Makes the project target <paramref name="moniker"/> ("net11.0"): changes the value of its one
    /// &lt;TargetFramework&gt; when that is a plain .NET version ("net10.0") and a different one. A project with
    /// several frameworks (&lt;TargetFrameworks&gt;), a platform version ("net10.0-windows") or more than one
    /// &lt;TargetFramework&gt; was set up that way on purpose and is left as it is. Returns the new content;
    /// <paramref name="oldMoniker"/> is the version it had.
    /// </summary>
    public static string SetTargetFramework(string projectFileContent, string moniker, out bool changed, out string oldMoniker)
    {
        changed = false;
        oldMoniker = null;
        var comments = GetCommentRanges(projectFileContent);
        var plurals = _targetFrameworks.Matches(projectFileContent);
        for(var i = 0; i < plurals.Count; i++)
        {
            if(!IsInside(comments, plurals[i].Index))
                return projectFileContent;
        }

        var found = (Group)null;
        var matches = _targetFramework.Matches(projectFileContent);
        for(var i = 0; i < matches.Count; i++)
        {
            if(IsInside(comments, matches[i].Index))
                continue;

            if(found != null)
                return projectFileContent;

            found = matches[i].Groups["tfm"];
        }

        if(found == null || !_plainNetVersion.IsMatch(found.Value) || string.Equals(found.Value, moniker, StringComparison.OrdinalIgnoreCase))
            return projectFileContent;

        changed = true;
        oldMoniker = found.Value;
        return projectFileContent.Substring(0, found.Index) + moniker + projectFileContent.Substring(found.Index + found.Length);
    }

    private static string AddProjectReference(string projectFileContent, string relativePath)
    {
        var newLine = projectFileContent.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var line = $"    <ProjectReference Include=\"{relativePath}\" />";

        //add to the item group that already holds project references (not one inside a comment)
        var references = GetProjectReferences(projectFileContent);
        if(references.Count > 0)
        {
            var lastReference = references[references.Count - 1].Index;
            var closing = projectFileContent.IndexOf("</ItemGroup>", lastReference, StringComparison.OrdinalIgnoreCase);
            if(closing > -1)
            {
                var lineStart = projectFileContent.LastIndexOf('\n', closing) + 1;
                return projectFileContent.Substring(0, lineStart) + line + newLine + projectFileContent.Substring(lineStart);
            }
        }

        //otherwise add a new item group before the end of the project
        var end = projectFileContent.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
        if(end < 0)
            throw new InvalidOperationException("The project file has no </Project> element.");

        var itemGroup = $"  <ItemGroup>{newLine}{line}{newLine}  </ItemGroup>{newLine}{newLine}";
        return projectFileContent.Substring(0, end) + itemGroup + projectFileContent.Substring(end);
    }

    /// <summary>
    /// Where the XML comments are: start and end (exclusive) of each &lt;!-- ... --&gt;.
    /// </summary>
    private static List<int[]> GetCommentRanges(string content)
    {
        var result = new List<int[]>();
        var start = content.IndexOf("<!--", StringComparison.Ordinal);
        while(start > -1)
        {
            var end = content.IndexOf("-->", start + 4, StringComparison.Ordinal);
            end = end < 0 ? content.Length : end + 3;
            result.Add(new[] { start, end });
            start = content.IndexOf("<!--", end, StringComparison.Ordinal);
        }

        return result;
    }

    private static bool IsInside(List<int[]> ranges, int index)
    {
        foreach(var range in ranges)
        {
            if(index >= range[0] && index < range[1])
                return true;
        }

        return false;
    }

    /// <summary>
    /// The project name in a project path: the file name without extension, with either separator.
    /// </summary>
    public static string GetProjectName(string projectPath)
    {
        var path = NormalizeSeparators(projectPath);
        var slash = path.LastIndexOf('/');
        var fileName = slash > -1 ? path.Substring(slash + 1) : path;
        var dot = fileName.LastIndexOf('.');
        return dot > 0 ? fileName.Substring(0, dot) : fileName;
    }

    private static string NormalizeSeparators(string path)
    {
        return path.Replace('\\', '/');
    }
}

/// <summary>
/// One project reference in a project file.
/// </summary>
public sealed class ProjectReferenceInfo
{
    public string Include { get; set; }
    public string Name { get; set; }
    public int Index { get; set; }
}

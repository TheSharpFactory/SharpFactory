#region Usings
using SF.Engine.Settings;
using SF.Engine.Sync;
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace SF.Engine.Targets;

/// <summary>
/// One solution Sync can target, and why it is suggested.
/// </summary>
public sealed class SolutionCandidate
{
    public string SolutionPath { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// The IDE that has it open; null when none does (as far as the scan can tell).
    /// </summary>
    public string OpenIn { get; set; }

    /// <summary>
    /// When this model was last synced to it; null when never.
    /// </summary>
    public DateTime? LastSynced { get; set; }

    /// <summary>
    /// True when it is in the model file's folder or one above it.
    /// </summary>
    public bool NearModel { get; set; }

    public bool IsOpen
    {
        get
        {
            return OpenIn != null;
        }
    }

    /// <summary>
    /// One line for the list: "Open in Visual Studio · synced 2026-11-05 14:10 · next to the model".
    /// </summary>
    public string Description
    {
        get
        {
            var parts = new List<string>(3);
            if(OpenIn != null)
                parts.Add($"Open in {OpenIn}");

            if(LastSynced.HasValue)
                parts.Add($"Synced {LastSynced.Value:yyyy-MM-dd HH:mm}");

            if(NearModel)
                parts.Add("Near the model file");

            return string.Join(" · ", parts);
        }
    }
}

/// <summary>
/// Builds Sync's target list: solutions open in an IDE first, then the ones this model was synced to (most
/// recent first), then the ones near the .edmj. An IDE that shows only a solution's name (in its window title) is
/// matched against the known solutions, then against the IDEs' recent-solution lists.
/// </summary>
public static class TargetSolutionFinder
{
    private const int _parentLevels = 3;

    public static List<SolutionCandidate> Find(string modelFile, AppSettings settings, List<OpenSolutionHint> openSolutions, List<string> recentSolutions)
    {
        return Find(modelFile, settings, openSolutions, recentSolutions, FindNearModel(modelFile));
    }

    /// <summary>
    /// The same, with the solutions near the model already found (<see cref="FindNearModel"/>): the slow searches (open
    /// IDEs, recent lists, folders) can run on another thread, and the list is built where the settings live.
    /// </summary>
    public static List<SolutionCandidate> Find(string modelFile, AppSettings settings, List<OpenSolutionHint> openSolutions, List<string> recentSolutions, List<string> nearModel)
    {
        var byPath = new Dictionary<string, SolutionCandidate>(SolutionProjectHost.PathComparer);
        var ordered = new List<SolutionCandidate>();

        foreach(var target in AppSettingsStore.GetSyncTargets(settings, modelFile))
        {
            if(!File.Exists(target.SolutionPath))
                continue;

            var candidate = GetOrAdd(byPath, ordered, target.SolutionPath);
            if(!candidate.LastSynced.HasValue || candidate.LastSynced.Value < target.LastSynced)
                candidate.LastSynced = target.LastSynced;
        }

        foreach(var solution in nearModel)
            GetOrAdd(byPath, ordered, solution).NearModel = true;

        //open solutions: by path when the command line named it, by name otherwise
        foreach(var hint in openSolutions)
        {
            if(hint.SolutionPath != null)
            {
                GetOrAdd(byPath, ordered, hint.SolutionPath).OpenIn = hint.IdeName;
                continue;
            }

            var matched = false;
            foreach(var candidate in ordered)
            {
                if(string.Equals(candidate.Name, hint.SolutionName, StringComparison.OrdinalIgnoreCase))
                {
                    candidate.OpenIn = hint.IdeName;
                    matched = true;
                }
            }

            if(!matched)
            {
                var recent = FindRecent(recentSolutions, hint.SolutionName);
                if(recent != null)
                    GetOrAdd(byPath, ordered, recent).OpenIn = hint.IdeName;
            }
        }

        ordered.Sort(CompareCandidates);
        return ordered;
    }

    /// <summary>
    /// The most recent solution with this name (file name without extension); null when there is none.
    /// </summary>
    private static string FindRecent(List<string> recentSolutions, string solutionName)
    {
        if(recentSolutions == null || string.IsNullOrWhiteSpace(solutionName))
            return null;

        foreach(var path in recentSolutions)
        {
            if(string.Equals(Path.GetFileNameWithoutExtension(path), solutionName, StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }

    /// <summary>
    /// .sln and .slnx files in the model file's folder and up to three folders above it.
    /// </summary>
    public static List<string> FindNearModel(string modelFile)
    {
        var result = new List<string>();
        if(string.IsNullOrWhiteSpace(modelFile))
            return result;

        var folder = Path.GetDirectoryName(Path.GetFullPath(modelFile));
        for(var level = 0; level <= _parentLevels && !string.IsNullOrEmpty(folder); level++)
        {
            result.AddRange(RunningIdeScanner.FindSolutionsInFolder(folder));
            folder = Path.GetDirectoryName(folder);
        }

        return result;
    }

    private static SolutionCandidate GetOrAdd(Dictionary<string, SolutionCandidate> byPath, List<SolutionCandidate> ordered, string path)
    {
        var fullPath = Path.GetFullPath(path);
        if(byPath.TryGetValue(fullPath, out var existing))
            return existing;

        var candidate = new SolutionCandidate
        {
            SolutionPath = fullPath,
            Name = Path.GetFileNameWithoutExtension(fullPath),
        };

        byPath.Add(fullPath, candidate);
        ordered.Add(candidate);
        return candidate;
    }

    private static int CompareCandidates(SolutionCandidate one, SolutionCandidate two)
    {
        if(one.IsOpen != two.IsOpen)
            return one.IsOpen ? -1 : 1;

        if(one.LastSynced.HasValue != two.LastSynced.HasValue)
            return one.LastSynced.HasValue ? -1 : 1;

        if(one.LastSynced.HasValue && two.LastSynced.HasValue && one.LastSynced.Value != two.LastSynced.Value)
            return two.LastSynced.Value.CompareTo(one.LastSynced.Value);

        return string.Compare(one.SolutionPath, two.SolutionPath, StringComparison.OrdinalIgnoreCase);
    }
}

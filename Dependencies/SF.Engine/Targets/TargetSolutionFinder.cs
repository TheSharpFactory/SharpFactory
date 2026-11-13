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
    /// When this model was last synced to it; null when never.
    /// </summary>
    public DateTime? LastSynced { get; set; }

    /// <summary>
    /// How many folders up from the model file it is: 0 in the model file's folder, 1 in the folder above, and so
    /// on; null when it is not near the model.
    /// </summary>
    public int? NearModelLevel { get; set; }

    public bool NearModel
    {
        get
        {
            return NearModelLevel.HasValue;
        }
    }

    /// <summary>
    /// One line for the list: "Synced 2026-11-05 14:10 · Near the model file".
    /// </summary>
    public string Description
    {
        get
        {
            var parts = new List<string>(2);
            if(LastSynced.HasValue)
                parts.Add($"Synced {LastSynced.Value:yyyy-MM-dd HH:mm}");

            if(NearModel)
                parts.Add("Near the model file");

            return string.Join(" · ", parts);
        }
    }
}

/// <summary>
/// Builds Sync's target list: the solutions this model was synced to (most recent first), then the ones near the
/// .edmj (the nearest first). Nothing is guessed from running programs: an IDE's command line still names the
/// solution it was started with after another one is opened in it.
/// </summary>
public static class TargetSolutionFinder
{
    private const int _parentLevels = 3;

    public static List<SolutionCandidate> Find(string modelFile, AppSettings settings)
    {
        return Find(modelFile, settings, FindNearModel(modelFile));
    }

    /// <summary>
    /// The same, with the solutions near the model already found (<see cref="FindNearModel"/>): the folder search can
    /// run on another thread, and the list is built where the settings live.
    /// </summary>
    public static List<SolutionCandidate> Find(string modelFile, AppSettings settings, List<string> nearModel)
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

        var modelFolder = string.IsNullOrWhiteSpace(modelFile) ? null : Path.GetDirectoryName(Path.GetFullPath(modelFile));
        foreach(var solution in nearModel)
        {
            var candidate = GetOrAdd(byPath, ordered, solution);
            candidate.NearModelLevel = CountLevelsUp(modelFolder, Path.GetDirectoryName(candidate.SolutionPath));
        }

        ordered.Sort(CompareCandidates);
        return ordered;
    }

    /// <summary>
    /// The solution Sync suggests: the one this model was last synced to; when it was never synced, the solution
    /// nearest the model file; otherwise none (null), and the user chooses.
    /// </summary>
    public static SolutionCandidate FindSuggested(List<SolutionCandidate> candidates)
    {
        if(candidates == null || candidates.Count == 0)
            return null;

        var first = candidates[0];
        return first.LastSynced.HasValue || first.NearModel ? first : null;
    }

    /// <summary>
    /// The solution this model was last synced to (when it is still there), or null.
    /// </summary>
    public static string FindLastSynced(string modelFile, AppSettings settings)
    {
        var last = (SyncTarget)null;
        foreach(var target in AppSettingsStore.GetSyncTargets(settings, modelFile))
        {
            if(File.Exists(target.SolutionPath) && (last == null || target.LastSynced > last.LastSynced))
                last = target;
        }

        return last == null ? null : Path.GetFullPath(last.SolutionPath);
    }

    /// <summary>
    /// True when both name the same file, compared like the file system (case-insensitive on Windows and macOS).
    /// </summary>
    public static bool IsSamePath(string one, string two)
    {
        if(string.IsNullOrWhiteSpace(one) || string.IsNullOrWhiteSpace(two))
            return false;

        return SolutionProjectHost.PathComparer.Equals(Path.GetFullPath(one), Path.GetFullPath(two));
    }

    /// <summary>
    /// True when the model file is in the solution's folder or below it: a model and its solution live together.
    /// </summary>
    public static bool IsModelInSolutionFolder(string modelFile, string solutionPath)
    {
        if(string.IsNullOrWhiteSpace(modelFile) || string.IsNullOrWhiteSpace(solutionPath))
            return false;

        var solutionFolder = Path.GetDirectoryName(Path.GetFullPath(solutionPath));
        var relative = Path.GetRelativePath(solutionFolder, Path.GetFullPath(modelFile));
        return !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
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
            result.AddRange(FindSolutionsInFolder(folder));
            folder = Path.GetDirectoryName(folder);
        }

        return result;
    }

    public static bool IsSolutionFile(string path)
    {
        return path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> FindSolutionsInFolder(string folder)
    {
        var result = new List<string>();
        try
        {
            foreach(var file in Directory.EnumerateFiles(folder, "*.sln", SearchOption.TopDirectoryOnly))
                result.Add(Path.GetFullPath(file));

            foreach(var file in Directory.EnumerateFiles(folder, "*.slnx", SearchOption.TopDirectoryOnly))
                result.Add(Path.GetFullPath(file));
        }
        catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
        {
            //no access to the folder
        }

        return result;
    }

    /// <summary>
    /// How many folders <paramref name="ancestor"/> is above <paramref name="folder"/> (0 for the same folder).
    /// </summary>
    private static int CountLevelsUp(string folder, string ancestor)
    {
        var level = 0;
        var current = folder;
        while(!string.IsNullOrEmpty(current) && !SolutionProjectHost.PathComparer.Equals(current, ancestor))
        {
            current = Path.GetDirectoryName(current);
            level++;
        }

        return level;
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

    /// <summary>
    /// Synced before first (the most recent first), then near the model (the nearest first), then by path.
    /// </summary>
    private static int CompareCandidates(SolutionCandidate one, SolutionCandidate two)
    {
        if(one.LastSynced.HasValue != two.LastSynced.HasValue)
            return one.LastSynced.HasValue ? -1 : 1;

        if(one.LastSynced.HasValue && two.LastSynced.HasValue && one.LastSynced.Value != two.LastSynced.Value)
            return two.LastSynced.Value.CompareTo(one.LastSynced.Value);

        if(one.NearModel != two.NearModel)
            return one.NearModel ? -1 : 1;

        if(one.NearModel && two.NearModel && one.NearModelLevel.Value != two.NearModelLevel.Value)
            return one.NearModelLevel.Value.CompareTo(two.NearModelLevel.Value);

        return string.Compare(one.SolutionPath, two.SolutionPath, StringComparison.OrdinalIgnoreCase);
    }
}

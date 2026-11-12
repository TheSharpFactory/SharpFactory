#region Usings
using Newtonsoft.Json;
using SF.Common.Logging;
using SF.Engine.Sync;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#endregion

namespace SF.Engine.Settings;

/// <summary>
/// Loads and saves AppSettings as JSON in the per-user config folder.
/// </summary>
public static class AppSettingsStore
{
    private const int _maxRecentFiles = 10;
    private static readonly Lock _syncRoot = new Lock();

    public static AppSettings Load()
    {
        lock(_syncRoot)
        {
            try
            {
                var file = AppPaths.SettingsFile;
                if(!File.Exists(file))
                    return new AppSettings();

                var settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(file)) ?? new AppSettings();

                //a list written as null (hand-edited, or an older version) reads as empty
                settings.RecentFiles ??= new List<string>();
                settings.SyncTargets ??= new List<SyncTarget>();
                settings.PaneWidths ??= new Dictionary<string, double>();
                return settings;
            }
            catch(Exception ex)
            {
                //unreadable settings are not worth stopping the app for: start fresh
                LogManager.WriteErrorLog(ex, "Unable to read the app settings");
                return new AppSettings();
            }
        }
    }

    public static void Save(AppSettings settings)
    {
        lock(_syncRoot)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.ConfigFolder);
                IOUtils.SaveFile(AppPaths.SettingsFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch(Exception ex)
            {
                LogManager.WriteErrorLog(ex, "Unable to save the app settings");
            }
        }
    }

    /// <summary>
    /// An independent copy of the settings, for work on another thread: the original can change meanwhile (the UI
    /// records a Sync target) without the copy changing under it.
    /// </summary>
    public static AppSettings Copy(AppSettings settings)
    {
        var copy = new AppSettings
        {
            RecentFiles = [.. settings.RecentFiles],
            SyncTargets = new List<SyncTarget>(settings.SyncTargets.Count),
            PaneWidths = new Dictionary<string, double>(settings.PaneWidths),
        };

        foreach(var target in settings.SyncTargets)
            copy.SyncTargets.Add(new SyncTarget { ModelFile = target.ModelFile, SolutionPath = target.SolutionPath, LastSynced = target.LastSynced });

        return copy;
    }

    /// <summary>
    /// Puts a file at the top of the recent files.
    /// </summary>
    public static void AddRecentFile(AppSettings settings, string file)
    {
        var fullPath = Path.GetFullPath(file);
        RemovePath(settings.RecentFiles, fullPath);
        settings.RecentFiles.Insert(0, fullPath);

        while(settings.RecentFiles.Count > _maxRecentFiles)
            settings.RecentFiles.RemoveAt(settings.RecentFiles.Count - 1);
    }

    public static void RemoveRecentFile(AppSettings settings, string file)
    {
        RemovePath(settings.RecentFiles, Path.GetFullPath(file));
    }

    /// <summary>
    /// Remembers that a model file was synced to a solution now.
    /// </summary>
    public static void RecordSyncTarget(AppSettings settings, string modelFile, string solutionPath)
    {
        var model = Path.GetFullPath(modelFile);
        var solution = Path.GetFullPath(solutionPath);

        foreach(var target in settings.SyncTargets)
        {
            if(SolutionProjectHost.PathComparer.Equals(target.ModelFile, model) && SolutionProjectHost.PathComparer.Equals(target.SolutionPath, solution))
            {
                target.LastSynced = DateTime.Now;
                return;
            }
        }

        settings.SyncTargets.Add(new SyncTarget { ModelFile = model, SolutionPath = solution, LastSynced = DateTime.Now });
    }

    /// <summary>
    /// The solutions a model file was synced to, most recent first.
    /// </summary>
    public static List<SyncTarget> GetSyncTargets(AppSettings settings, string modelFile)
    {
        var result = new List<SyncTarget>();
        if(string.IsNullOrWhiteSpace(modelFile))
            return result;

        var model = Path.GetFullPath(modelFile);
        foreach(var target in settings.SyncTargets)
        {
            if(SolutionProjectHost.PathComparer.Equals(target.ModelFile, model))
                result.Add(target);
        }

        result.Sort(CompareByLastSyncedDescending);
        return result;
    }

    private static int CompareByLastSyncedDescending(SyncTarget one, SyncTarget two)
    {
        return two.LastSynced.CompareTo(one.LastSynced);
    }

    private static void RemovePath(List<string> paths, string path)
    {
        for(var i = paths.Count - 1; i >= 0; i--)
        {
            if(SolutionProjectHost.PathComparer.Equals(paths[i], path))
                paths.RemoveAt(i);
        }
    }
}

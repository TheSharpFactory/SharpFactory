#region Usings
using System;
using System.Collections.Generic;
#endregion

namespace SF.Engine.Settings;

/// <summary>
/// The user's app settings. Kept outside the .edmj so the model file format does not change.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Recently opened .edmj files, newest first.
    /// </summary>
    public List<string> RecentFiles { get; set; } = new List<string>();

    /// <summary>
    /// Solutions each .edmj was synced to.
    /// </summary>
    public List<SyncTarget> SyncTargets { get; set; } = new List<SyncTarget>();

    /// <summary>
    /// The widths the user dragged the split panes to, by pane ("Models.Tree", "Sync.Files").
    /// </summary>
    public Dictionary<string, double> PaneWidths { get; set; } = new Dictionary<string, double>();
}

/// <summary>
/// One solution a model file was synced to, and when.
/// </summary>
public sealed class SyncTarget
{
    public string ModelFile { get; set; }
    public string SolutionPath { get; set; }
    public DateTime LastSynced { get; set; }
}

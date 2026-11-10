#region Usings
using SF.Models;
using System.Collections.Generic;
#endregion

namespace SF.Engine.Sync;

/// <summary>
/// What to sync, and where.
/// </summary>
public sealed class SyncRequest
{
    public ModelContainer ModelContainer { get; set; }

    /// <summary>
    /// The .sln or .slnx to sync into. Created when it does not exist.
    /// </summary>
    public string SolutionPath { get; set; }

    /// <summary>
    /// When true nothing is written: the report lists what Sync would do.
    /// </summary>
    public bool Preview { get; set; }
}

public enum SyncFileChangeKind
{
    Added = 0,
    Changed = 1,
    Deleted = 2,
}

/// <summary>
/// One generated file Sync adds, changes or deletes.
/// </summary>
public sealed class SyncFileChange
{
    public string FullPath { get; set; }
    public string RelativePath { get; set; }
    public SyncFileChangeKind Kind { get; set; }

    /// <summary>
    /// The current content (changed and deleted files, preview only).
    /// </summary>
    public string OldContent { get; set; }

    /// <summary>
    /// The new content (added and changed files, preview only).
    /// </summary>
    public string NewContent { get; set; }
}

/// <summary>
/// The number of generated files in one project.
/// </summary>
public sealed class ProjectFileCount
{
    public string ProjectName { get; set; }
    public int FileCount { get; set; }
}

/// <summary>
/// What Sync did (or, for a preview, would do).
/// </summary>
public sealed class SyncReport
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public bool IsPreview { get; set; }
    public string SolutionPath { get; set; }
    public bool SolutionCreated { get; set; }
    public bool SolutionChanged { get; set; }
    public List<string> ProjectsCreated { get; set; } = new List<string>();

    /// <summary>
    /// Projects whose project file was already on disk: added to the solution, their project file kept.
    /// </summary>
    public List<string> ExistingProjectsAdded { get; set; } = new List<string>();
    public List<string> ProjectsMoved { get; set; } = new List<string>();
    public List<string> ProjectFilesChanged { get; set; } = new List<string>();
    public List<string> ReferencesAdded { get; set; } = new List<string>();

    /// <summary>
    /// References (in any project of the solution) pointed at a project's new place after it moved.
    /// </summary>
    public List<string> ReferencesUpdated { get; set; } = new List<string>();
    public List<SyncFileChange> FileChanges { get; set; } = new List<SyncFileChange>();
    public int UnchangedFileCount { get; set; }
    public List<ProjectFileCount> ProjectFileCounts { get; set; } = new List<ProjectFileCount>();

    /// <summary>
    /// Files at a generated path that are not Sharp Factory files (no marker on the first line): Sync does not
    /// overwrite them.
    /// </summary>
    public List<string> UnmarkedFiles { get; set; } = new List<string>();

    public int CountFiles(SyncFileChangeKind kind)
    {
        var count = 0;
        foreach(var change in FileChanges)
        {
            if(change.Kind == kind)
                count++;
        }

        return count;
    }

    /// <summary>
    /// True when Sync has nothing to do.
    /// </summary>
    public bool IsUpToDate
    {
        get
        {
            return FileChanges.Count == 0 && ProjectsCreated.Count == 0 && ExistingProjectsAdded.Count == 0 && ProjectsMoved.Count == 0
                && ReferencesAdded.Count == 0 && ReferencesUpdated.Count == 0 && !SolutionChanged && UnmarkedFiles.Count == 0;
        }
    }
}

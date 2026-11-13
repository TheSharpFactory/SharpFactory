#region Usings
using SF.Common.Logging;
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace SF.Engine.Sync;

/// <summary>
/// Sync without Visual Studio: generates every file of a model container into a .sln or .slnx, creating and
/// referencing the projects and removing generated files that are no longer in the model.
/// </summary>
public static class SolutionSync
{
    private const string _notASharpFactoryFile = " (not a Sharp Factory file)";
    private static readonly HashSet<string> _excludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "properties", "node_modules" };

    public static SyncReport Run(SyncRequest request, IProgressLog log)
    {
        var report = new SyncReport { IsPreview = request.Preview };

        var unsupported = ModelNameCheck.Find(request.ModelContainer);
        if(unsupported.Count > 0)
        {
            report.SolutionPath = Path.GetFullPath(request.SolutionPath);
            report.ErrorMessage = "Nothing was written. These objects are not supported: " + string.Join("; ", unsupported) + ". Update the model to remove them.";
            log.Error($"Synchronization stopped before writing anything..... {report.ErrorMessage}", report.ErrorMessage);
            return report;
        }

        var badNames = ModelValidation.ValidatePathNames(request.ModelContainer);
        if(badNames.Count > 0)
        {
            report.SolutionPath = Path.GetFullPath(request.SolutionPath);
            report.ErrorMessage = "Nothing was written. These names cannot be used in file paths: " + string.Join("; ", badNames);
            log.Error($"Synchronization stopped before writing anything..... {report.ErrorMessage}", report.ErrorMessage);
            return report;
        }

        //a real Sync first checks that every file it would change can be written, so it never stops half-way
        if(!request.Preview)
        {
            var blocked = FindBlockedFiles(request, out var failedPreview);
            if(failedPreview != null)
            {
                report.SolutionPath = failedPreview.SolutionPath;
                report.ErrorMessage = failedPreview.ErrorMessage;
                log.Error($"Synchronization stopped before writing anything..... {report.ErrorMessage}", report.ErrorMessage);
                return report;
            }

            if(blocked.Count > 0)
            {
                report.SolutionPath = Path.GetFullPath(request.SolutionPath);
                report.ErrorMessage = DescribeBlocked(blocked);
                log.Error($"Synchronization stopped before writing anything..... {report.ErrorMessage}", report.ErrorMessage);
                return report;
            }
        }

        try
        {
            report.SolutionPath = Path.GetFullPath(request.SolutionPath);
            var solution = SolutionFile.OpenOrCreate(report.SolutionPath);
            report.SolutionCreated = solution.IsNew;

            log.Info(solution.IsNew ? $"Creating Solution {solution.FullPath}" : $"Opening Solution {solution.FullPath}");

            var host = new SolutionProjectHost(solution, request.ModelContainer, report, request.Preview, log);

            IOUtils.CreateFolders = !request.Preview;
            var generation = GenerationRunner.Run(request.ModelContainer, host, log);

            log.Info("Writing Project Files....");
            host.WriteProjectFiles();

            log.Info(request.Preview ? "Finding Stale Files...." : "Deleting Stale Files....");
            var deleted = RemoveStaleFiles(generation, solution.Folder, report, request.Preview);
            log.Info($"Stale Files {(request.Preview ? "To Delete" : "Deleted")}: {deleted}");

            report.SolutionChanged = solution.Changed;
            if(solution.Changed && !request.Preview)
            {
                log.Info("Saving Solution....");
                solution.Save();
            }

            log.Info("Generating Final Report....");
            CountFilesPerProject(generation, report);
            foreach(var item in report.ProjectFileCounts)
                log.Info($"Project {item.ProjectName}. File count {item.FileCount}");

            log.Info($"Files added: {report.CountFiles(SyncFileChangeKind.Added)}, changed: {report.CountFiles(SyncFileChangeKind.Changed)}, deleted: {report.CountFiles(SyncFileChangeKind.Deleted)}, unchanged: {report.UnchangedFileCount}");

            //only a preview gets here with such files: a real Sync stops before writing anything
            foreach(var unmarked in report.UnmarkedFiles)
                log.Info($"Not a Sharp Factory file, Sync will stop: {unmarked}");
            log.Info(request.Preview ? "Preview Completed Successfully!" : "Synchronization Completed Successfully!");
            report.Success = true;
        }
        catch(Exception ex)
        {
            LogManager.WriteErrorLog(ex, "Sharp Factory Sync Exception");
            log.Error($"Synchronization failed..... {ex.Message}", "There was an error. Synchronization has failed!");
            report.Success = false;
            report.ErrorMessage = ex.Message;
        }
        finally
        {
            IOUtils.CreateFolders = true;
        }

        return report;
    }

    /// <summary>
    /// Runs a preview (nothing is written) and returns the files Sync would change or delete, project files and the
    /// solution included, that cannot be written now: "Entity\Customer.cs (read-only)". Empty when all can be, or when
    /// the preview itself fails (the Sync then reports that error).
    /// </summary>
    internal static List<string> FindBlockedFiles(SyncRequest request, out SyncReport failedPreview)
    {
        failedPreview = null;
        var result = new List<string>();
        var preview = Run(new SyncRequest { ModelContainer = request.ModelContainer, SolutionPath = request.SolutionPath, Preview = true }, new NullProgressLog());
        if(!preview.Success)
        {
            failedPreview = preview;
            return result;
        }

        var solutionFolder = Path.GetDirectoryName(preview.SolutionPath);

        foreach(var unmarked in preview.UnmarkedFiles)
            result.Add(Path.GetRelativePath(solutionFolder, unmarked) + _notASharpFactoryFile);

        var paths = new List<string>();
        foreach(var change in preview.FileChanges)
        {
            if(change.Kind != SyncFileChangeKind.Added)
                paths.Add(change.FullPath ?? Path.Combine(solutionFolder, change.RelativePath));
        }

        foreach(var projectFile in preview.ProjectFilesChanged)
            paths.Add(Path.Combine(solutionFolder, projectFile));

        if(preview.SolutionChanged)
            paths.Add(preview.SolutionPath);

        foreach(var path in paths)
        {
            var reason = WhyNotWritable(path);
            if(reason != null)
                result.Add($"{Path.GetRelativePath(solutionFolder, path)} ({reason})");
        }

        return result;
    }

    /// <summary>
    /// Null when the file can be written (or does not exist yet); otherwise why not.
    /// </summary>
    private static string WhyNotWritable(string path)
    {
        if(!File.Exists(path))
            return null;

        if((File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
            return "read-only";

        try
        {
            using(new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }
        }
        catch(UnauthorizedAccessException)
        {
            return "access denied";
        }
        catch(IOException)
        {
            return "in use by another program";
        }

        return null;
    }

    private static string DescribeBlocked(List<string> blocked)
    {
        var shown = new List<string>();
        for(var i = 0; i < blocked.Count && i < 5; i++)
            shown.Add(blocked[i]);

        var more = blocked.Count > 5 ? $" and {blocked.Count - 5} more" : "";
        var files = blocked.Count == 1 ? "This file cannot" : $"These {blocked.Count} files cannot";

        //a hand-written file at a generated path is never replaced: it has to be moved or renamed
        var hasUnmarked = false;
        foreach(var path in blocked)
        {
            if(path.EndsWith(_notASharpFactoryFile, StringComparison.Ordinal))
                hasUnmarked = true;
        }

        var advice = hasUnmarked ? "Move or rename the files that are not Sharp Factory files, close the others or clear read-only, then Sync again." : "Close them or clear read-only, then Sync again.";
        return $"Nothing was written. {files} be written: {string.Join(", ", shown)}{more}. {advice}";
    }

    /// <summary>
    /// Deletes (or, in a preview, lists) the generated files in the model's projects that the model no longer
    /// produces. Only files that start with the Sharp Factory marker are touched; the project root, bin, obj,
    /// Properties and node_modules are skipped, and so are projects outside the solution's folder.
    /// </summary>
    private static int RemoveStaleFiles(GenerationResult generation, string solutionFolder, SyncReport report, bool preview)
    {
        var filesInModel = new HashSet<string>(SolutionProjectHost.PathComparer);
        foreach(var file in generation.Files)
            filesInModel.Add(file.FullPath);

        var count = 0;
        foreach(var projectFolder in generation.ProjectFolders.Values)
        {
            //another solution's project (outside this solution's folder): nothing is deleted in it
            if(!Directory.Exists(projectFolder) || !SolutionProjectHost.IsInFolder(projectFolder, solutionFolder))
                continue;

            foreach(var subFolder in Directory.EnumerateDirectories(projectFolder))//generated files are never in a project's root
                count += RemoveStaleFilesInFolder(subFolder, filesInModel, solutionFolder, report, preview);
        }

        return count;
    }

    private static int RemoveStaleFilesInFolder(string folder, HashSet<string> filesInModel, string solutionFolder, SyncReport report, bool preview)
    {
        if(_excludedFolders.Contains(Path.GetFileName(folder)))
            return 0;

        var count = 0;
        foreach(var file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.TopDirectoryOnly))
        {
            if(filesInModel.Contains(file) || !IsSharpFactoryFile(file))
                continue;

            var change = new SyncFileChange
            {
                FullPath = file,
                RelativePath = Path.GetRelativePath(solutionFolder, file),
                Kind = SyncFileChangeKind.Deleted,
            };

            if(preview)
                change.OldContent = File.ReadAllText(file);
            else
                File.Delete(file);

            report.FileChanges.Add(change);
            count++;
        }

        foreach(var subFolder in Directory.EnumerateDirectories(folder))
            count += RemoveStaleFilesInFolder(subFolder, filesInModel, solutionFolder, report, preview);

        return count;
    }

    /// <summary>
    /// Sharp Factory files start with the marker line; anything else in the projects belongs to the user.
    /// </summary>
    public static bool IsSharpFactoryFile(string file)
    {
        using(var reader = File.OpenText(file))
        {
            var firstLine = reader.ReadLine();
            return firstLine != null && string.CompareOrdinal(SF.CodeGeneration.Common.SharpFactoryFileMarker, firstLine.Trim()) == 0;
        }
    }

    private static void CountFilesPerProject(GenerationResult generation, SyncReport report)
    {
        foreach(var project in generation.Projects)
        {
            var projectFolder = generation.ProjectFolders[project.Key] + Path.DirectorySeparatorChar;
            var count = 0;
            foreach(var file in generation.Files)
            {
                if(file.FullPath.StartsWith(projectFolder, SolutionProjectHost.PathComparison))
                    count++;
            }

            report.ProjectFileCounts.Add(new ProjectFileCount { ProjectName = project.Value, FileCount = count });
        }
    }
}

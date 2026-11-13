#region Usings
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#endregion

namespace SF.Engine.Sync;

/// <summary>
/// Puts generated projects and files into a .sln or .slnx: creates missing projects (in the dependencies
/// solution folder), moves projects when that folder changed, adds project references, writes changed files.
/// A project of the solution that is outside the solution's folder (another solution's) is used where it is:
/// never moved, its project file never changed. In preview mode it only records what it would do.
/// </summary>
internal sealed class SolutionProjectHost : IProjectHost
{
    private readonly SolutionFile _solution;
    private readonly SyncReport _report;
    private readonly bool _preview;
    private readonly string _dependenciesFolder;
    private readonly string _projectTemplate;
    //the common Repository project (the SQL engine) is created with the Microsoft.Data.SqlClient package
    private readonly string _sqlClientProjectTemplate;
    private readonly string _commonRepositoryProject;
    private readonly IProgressLog _log;

    //project files read or created during this run, by full path; written at the end
    private readonly Dictionary<string, string> _projectFiles = new Dictionary<string, string>(PathComparer);
    private readonly HashSet<string> _changedProjectFiles = new HashSet<string>(PathComparer);
    //project files that start with a UTF-8 BOM: written back with it
    private readonly HashSet<string> _projectFilesWithBom = new HashSet<string>(PathComparer);
    //the folder returned for each project already prepared in this run
    private readonly Dictionary<string, string> _preparedProjects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    //projects moved in this run: name -> new project file
    private readonly Dictionary<string, string> _movedProjects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public SolutionProjectHost(SolutionFile solution, ModelContainer modelContainer, SyncReport report, bool preview, IProgressLog log)
    {
        _solution = solution;
        _report = report;
        _preview = preview;
        _log = log;
        _dependenciesFolder = string.IsNullOrWhiteSpace(modelContainer.CommonSettings.DependenciesFolder) ? null : modelContainer.CommonSettings.DependenciesFolder.Trim();
        var targetFramework = modelContainer.CommonSettings.TargetFramework ?? TargetFrameworks.NET10;
        _projectTemplate = ProjectFileTemplate.Compose(targetFramework, false);
        _sqlClientProjectTemplate = ProjectFileTemplate.Compose(targetFramework, true);
        _commonRepositoryProject = modelContainer.CommonLayers.Repository.FullProjectName(modelContainer.CommonSettings);
    }

    /// <summary>
    /// File paths compare like the file system: case-insensitive on Windows and macOS, case-sensitive on Linux.
    /// </summary>
    public static StringComparer PathComparer
    {
        get
        {
            return OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        }
    }

    /// <summary>
    /// The StringComparison that matches PathComparer.
    /// </summary>
    public static StringComparison PathComparison
    {
        get
        {
            return OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        }
    }

    public string PrepareProject(string projectName)
    {
        if(_preparedProjects.TryGetValue(projectName, out var preparedFolder))
            return preparedFolder;

        var expectedFolder = GetProjectFolder(projectName);
        var existing = _solution.FindProject(projectName);

        var folder = (string)null;
        if(existing == null)
            folder = CreateProject(projectName, expectedFolder);
        else if(!IsInFolder(existing.ProjectFolder, _solution.Folder))
            folder = UseProjectOutsideSolution(existing);
        else if(IsInDependenciesFolder(existing.SolutionFolderPath))
            folder = existing.ProjectFolder;
        else
            folder = MoveProject(existing, expectedFolder);

        _preparedProjects.Add(projectName, folder);
        return folder;
    }

    public void PrepareReferences(ModelContainer modelContainer, Model model)
    {
        foreach(var pair in ProjectReferenceRules.GetReferences(modelContainer, model))
            EnsureReference(pair.ProjectName, pair.ReferencedProjectName);
    }

    public void WriteFile(List<CodeFile> filesInModel, string folder, string fileName, string content)
    {
        if(string.IsNullOrWhiteSpace(content))
            return;

        var fullPath = Path.Combine(folder, fileName);

        //a hand-written file at a generated path is never overwritten
        if(File.Exists(fullPath) && !SolutionSync.IsSharpFactoryFile(fullPath))
        {
            _report.UnmarkedFiles.Add(fullPath);
            return;
        }

        filesInModel.Add(new CodeFile { FolderPath = folder, FullPath = fullPath, FileName = fileName });

        var newContent = IOUtils.ToCrlf(content);
        var exists = File.Exists(fullPath);
        var oldContent = exists ? File.ReadAllText(fullPath) : null;

        if(exists && string.Equals(oldContent, newContent, StringComparison.Ordinal))
        {
            _report.UnchangedFileCount++;
            return;
        }

        var change = new SyncFileChange
        {
            FullPath = fullPath,
            RelativePath = Path.GetRelativePath(_solution.Folder, fullPath),
            Kind = exists ? SyncFileChangeKind.Changed : SyncFileChangeKind.Added,
        };

        if(_preview)
        {
            change.OldContent = oldContent;
            change.NewContent = newContent;
        }
        else
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(fullPath, newContent);
        }

        _report.FileChanges.Add(change);
    }

    /// <summary>
    /// Writes the project files that changed during the run (new projects, added references), after pointing every
    /// project's references to moved projects at their new place.
    /// </summary>
    public void WriteProjectFiles()
    {
        FixReferencesToMovedProjects();

        foreach(var projectFile in _changedProjectFiles)
        {
            //another solution's project: left as it is
            if(!IsInFolder(projectFile, _solution.Folder))
            {
                _log.Info($"Not changed (outside the solution's folder): {projectFile}");
                continue;
            }

            _report.ProjectFilesChanged.Add(Path.GetRelativePath(_solution.Folder, projectFile));
            if(_preview)
                continue;

            Directory.CreateDirectory(Path.GetDirectoryName(projectFile));
            File.WriteAllText(projectFile, _projectFiles[projectFile], new UTF8Encoding(_projectFilesWithBom.Contains(projectFile)));
        }
    }

    private string CreateProject(string projectName, string projectFolder)
    {
        var projectFile = Path.Combine(projectFolder, projectName + ".csproj");

        //a project file already on disk (synced before into another solution, or edited by the user) is kept as it
        //is, packages included: it is only added to this solution
        if(File.Exists(projectFile))
        {
            _projectFiles[projectFile] = ReadProjectFile(projectFile);
            _solution.AddProject(projectFile, _dependenciesFolder);
            _report.ExistingProjectsAdded.Add(projectName);
            _log.Info($"Added existing project {projectName}");
            return projectFolder;
        }

        var template = string.Equals(projectName, _commonRepositoryProject, StringComparison.OrdinalIgnoreCase) ? _sqlClientProjectTemplate : _projectTemplate;
        _projectFiles[projectFile] = template;
        _changedProjectFiles.Add(projectFile);

        if(!_preview)
        {
            Directory.CreateDirectory(projectFolder);
            File.WriteAllText(projectFile, template);
        }

        _solution.AddProject(projectFile, _dependenciesFolder);
        _report.ProjectsCreated.Add(projectName);
        _log.Info($"Created project {projectName}");
        return projectFolder;
    }

    /// <summary>
    /// A generated project the solution has outside its folder, such as another solution's (a solution that
    /// references the data access of another): its files are written where it is. It is never moved: that would
    /// take it away from the other solution.
    /// </summary>
    private string UseProjectOutsideSolution(SolutionProjectInfo existing)
    {
        _report.ProjectsOutsideSolution.Add($"{existing.Name}: {existing.ProjectFolder}");
        _log.Info($"Project {existing.Name} is outside the solution's folder ({existing.ProjectFolder}): its files are written there, the project is not moved");
        return existing.ProjectFolder;
    }

    /// <summary>
    /// True when <paramref name="path"/> is <paramref name="folder"/> or inside it.
    /// </summary>
    public static bool IsInFolder(string path, string folder)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(folder), Path.GetFullPath(path));
        return !Path.IsPathRooted(relative) && !string.Equals(relative, "..", StringComparison.Ordinal)
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !relative.StartsWith("../", StringComparison.Ordinal);
    }

    private string MoveProject(SolutionProjectInfo existing, string newProjectFolder)
    {
        var projectName = existing.Name;
        var newProjectFile = Path.Combine(newProjectFolder, Path.GetFileName(existing.ProjectFile));

        if(!PathComparer.Equals(existing.ProjectFolder, newProjectFolder))
        {
            if(Directory.Exists(newProjectFolder) && Directory.GetFileSystemEntries(newProjectFolder).Length > 0)
                throw new InvalidOperationException($"Cannot move project {projectName} to {newProjectFolder}: that folder already exists and is not empty.");

            if(!_preview)
            {
                if(Directory.Exists(newProjectFolder))
                    Directory.Delete(newProjectFolder);

                Directory.CreateDirectory(Path.GetDirectoryName(newProjectFolder));
                Directory.Move(existing.ProjectFolder, newProjectFolder);
            }
        }

        _solution.MoveProject(existing, newProjectFile, _dependenciesFolder);
        _movedProjects[projectName] = newProjectFile;
        _report.ProjectsMoved.Add($"{projectName}: {DisplayFolder(existing.SolutionFolderPath)} -> {DisplayFolder(_dependenciesFolder)}");
        _log.Info($"Moved project {projectName} to {DisplayFolder(_dependenciesFolder)}");

        //in a preview the files are still in the old folder: compare against those
        return _preview ? existing.ProjectFolder : newProjectFolder;
    }

    private void EnsureReference(string projectName, string referencedProjectName)
    {
        var project = _solution.FindProject(projectName);
        var referenced = _solution.FindProject(referencedProjectName);
        if(project == null || referenced == null)
            return;//like the Visual Studio Sync: only between projects that exist

        //another solution's project file is left as it is
        if(!IsInFolder(project.ProjectFile, _solution.Folder))
            return;

        var content = GetProjectFileContent(project.ProjectFile);
        if(content == null)
            return;

        var relativePath = Path.GetRelativePath(project.ProjectFolder, referenced.ProjectFile).Replace('/', '\\');
        var newContent = ProjectFileEditor.SetProjectReference(content, referenced.Name, relativePath, out var changed);
        if(!changed)
            return;

        _projectFiles[project.ProjectFile] = newContent;
        _changedProjectFiles.Add(project.ProjectFile);
        _report.ReferencesAdded.Add($"{project.Name} -> {referenced.Name}");
    }

    /// <summary>
    /// Any project of the solution (the user's own ones too) that references a moved project gets the new path.
    /// </summary>
    private void FixReferencesToMovedProjects()
    {
        if(_movedProjects.Count == 0)
            return;

        foreach(var project in _solution.GetProjects())
        {
            //another solution's project file is left as it is
            if(!IsInFolder(project.ProjectFile, _solution.Folder))
                continue;

            var content = GetProjectFileContent(project.ProjectFile);
            if(content == null)
                continue;

            foreach(var reference in ProjectFileEditor.GetProjectReferences(content))
            {
                if(!_movedProjects.TryGetValue(reference.Name, out var movedProjectFile))
                    continue;

                var relativePath = Path.GetRelativePath(project.ProjectFolder, movedProjectFile).Replace('/', '\\');
                var newContent = ProjectFileEditor.SetProjectReference(content, reference.Name, relativePath, out var changed);
                if(!changed)
                    continue;

                content = newContent;
                _projectFiles[project.ProjectFile] = content;
                _changedProjectFiles.Add(project.ProjectFile);
                _report.ReferencesUpdated.Add($"{project.Name} -> {reference.Name}");
            }
        }
    }

    private string GetProjectFileContent(string projectFile)
    {
        if(_projectFiles.TryGetValue(projectFile, out var content))
            return content;

        if(!File.Exists(projectFile))
            return null;

        content = ReadProjectFile(projectFile);
        _projectFiles[projectFile] = content;
        return content;
    }

    /// <summary>
    /// The project file's text; remembers whether it starts with a BOM, so it is written back the same way.
    /// </summary>
    private string ReadProjectFile(string projectFile)
    {
        var bytes = File.ReadAllBytes(projectFile);
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        if(hasBom)
            _projectFilesWithBom.Add(projectFile);

        return hasBom ? Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3) : Encoding.UTF8.GetString(bytes);
    }

    private string GetProjectFolder(string projectName)
    {
        if(_dependenciesFolder == null)
            return Path.Combine(_solution.Folder, projectName);

        return Path.Combine(_solution.Folder, _dependenciesFolder, projectName);
    }

    /// <summary>
    /// True when a project in solution folder <paramref name="current"/> ("/a/b/", null for the root) is where the
    /// dependencies folder puts it: in that folder or anywhere below it.
    /// </summary>
    private bool IsInDependenciesFolder(string current)
    {
        var expected = SolutionFile.ToSolutionFolderPath(_dependenciesFolder);
        if(expected == null)
            return current == null;

        return current != null && current.StartsWith(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string DisplayFolder(string solutionFolder)
    {
        return string.IsNullOrWhiteSpace(solutionFolder) ? "(solution root)" : solutionFolder;
    }
}

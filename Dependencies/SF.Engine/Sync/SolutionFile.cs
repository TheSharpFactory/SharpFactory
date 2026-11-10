#region Usings
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#endregion

namespace SF.Engine.Sync;

/// <summary>
/// A .sln or .slnx file: find, add and move projects, then save. Nothing is written until Save.
/// </summary>
public sealed class SolutionFile
{
    private readonly ISolutionSerializer _serializer;
    private readonly SolutionModel _model;

    public string FullPath { get; set; }
    public string Folder { get; set; }
    public bool IsNew { get; set; }
    public bool Changed { get; set; }

    private SolutionFile(string fullPath, ISolutionSerializer serializer, SolutionModel model, bool isNew)
    {
        FullPath = fullPath;
        Folder = Path.GetDirectoryName(fullPath);
        _serializer = serializer;
        _model = model;
        IsNew = isNew;
        Changed = isNew;
    }

    /// <summary>
    /// True for the solution file types Sync supports.
    /// </summary>
    public static bool IsSupported(string path)
    {
        return SolutionSerializers.GetSerializerByMoniker(path) != null;
    }

    /// <summary>
    /// Opens an existing solution, or prepares a new one (saved on Save) when the file does not exist.
    /// </summary>
    public static SolutionFile OpenOrCreate(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var serializer = SolutionSerializers.GetSerializerByMoniker(fullPath);
        if(serializer == null)
            throw new ArgumentException($"Not a .sln or .slnx file: {fullPath}");

        if(File.Exists(fullPath))
        {
            var model = serializer.OpenAsync(fullPath, CancellationToken.None).GetAwaiter().GetResult();
            return new SolutionFile(fullPath, serializer, model, false);
        }

        var newModel = new SolutionModel();
        //a .sln lists its configurations; a .slnx without any uses Debug and Release on Any CPU
        newModel.AddBuildType("Debug");
        newModel.AddBuildType("Release");
        newModel.AddPlatform("Any CPU");
        return new SolutionFile(fullPath, serializer, newModel, true);
    }

    /// <summary>
    /// The project with this name (case-insensitive), in any solution folder; null when there is none.
    /// </summary>
    public SolutionProjectInfo FindProject(string projectName)
    {
        foreach(var project in _model.SolutionProjects)
        {
            if(string.Equals(GetName(project), projectName, StringComparison.OrdinalIgnoreCase))
                return ToInfo(project);
        }

        return null;
    }

    /// <summary>
    /// Every project in the solution.
    /// </summary>
    public List<SolutionProjectInfo> GetProjects()
    {
        var result = new List<SolutionProjectInfo>(_model.SolutionProjects.Count);
        foreach(var project in _model.SolutionProjects)
            result.Add(ToInfo(project));

        return result;
    }

    /// <summary>
    /// A dependencies folder as a solution-folder path: "src/Generated" or "src\Generated" becomes "/src/Generated/";
    /// empty becomes null (the solution root).
    /// </summary>
    public static string ToSolutionFolderPath(string solutionFolder)
    {
        if(string.IsNullOrWhiteSpace(solutionFolder))
            return null;

        var trimmed = solutionFolder.Replace('\\', '/').Trim().Trim('/');
        return trimmed.Length == 0 ? null : $"/{trimmed}/";
    }

    /// <summary>
    /// Adds a project, in the solution folder <paramref name="solutionFolder"/> (nested folders with '/' or '\')
    /// when it is not empty.
    /// </summary>
    public SolutionProjectInfo AddProject(string projectFileFullPath, string solutionFolder)
    {
        var folder = GetOrAddFolder(solutionFolder);
        var project = _model.AddProject(ToRelativePath(projectFileFullPath), null, folder);
        Changed = true;
        return ToInfo(project);
    }

    /// <summary>
    /// Points a project to its new location and solution folder.
    /// </summary>
    public SolutionProjectInfo MoveProject(SolutionProjectInfo projectInfo, string newProjectFileFullPath, string solutionFolder)
    {
        _model.RemoveProject(projectInfo.Model);
        return AddProject(newProjectFileFullPath, solutionFolder);
    }

    public void Save()
    {
        if(!Changed)
            return;

        if(!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);

        _serializer.SaveAsync(FullPath, _model, CancellationToken.None).GetAwaiter().GetResult();
        Changed = false;
        IsNew = false;
    }

    private SolutionFolderModel GetOrAddFolder(string solutionFolder)
    {
        var path = ToSolutionFolderPath(solutionFolder);
        if(path == null)
            return null;

        return _model.AddFolder(path);
    }

    private string ToRelativePath(string fullPath)
    {
        //the operating system's separator: the serializers write .slnx with '/' and .sln in its usual form
        return Path.GetRelativePath(Folder, fullPath);
    }

    /// <summary>
    /// The project's name from its file path, with either separator (a path read on another OS can use the other one).
    /// </summary>
    private static string GetName(SolutionProjectModel project)
    {
        return ProjectFileEditor.GetProjectName(project.FilePath);
    }

    private SolutionProjectInfo ToInfo(SolutionProjectModel project)
    {
        var relativePath = project.FilePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(Folder, relativePath));
        return new SolutionProjectInfo
        {
            Name = GetName(project),
            ProjectFile = fullPath,
            ProjectFolder = Path.GetDirectoryName(fullPath),
            TopSolutionFolder = GetTopFolderName(project),
            SolutionFolderPath = GetFolderPath(project),
            Model = project,
        };
    }

    /// <summary>
    /// The project's solution folder as a path ("/src/Generated/"); null at the solution root.
    /// </summary>
    private static string GetFolderPath(SolutionItemModel item)
    {
        var folder = item.Parent;
        if(folder == null)
            return null;

        var path = "/";
        while(folder != null)
        {
            path = "/" + folder.Name + path;
            folder = folder.Parent;
        }

        return path;
    }

    private static string GetTopFolderName(SolutionItemModel item)
    {
        var folder = item.Parent;
        if(folder == null)
            return null;

        while(folder.Parent != null)
            folder = folder.Parent;

        return folder.Name;
    }
}

/// <summary>
/// A project in a solution.
/// </summary>
public sealed class SolutionProjectInfo
{
    public string Name { get; set; }
    public string ProjectFile { get; set; }
    public string ProjectFolder { get; set; }

    /// <summary>
    /// The top-level solution folder the project is in; null at the solution root.
    /// </summary>
    public string TopSolutionFolder { get; set; }

    /// <summary>
    /// The full solution-folder path the project is in ("/src/Generated/"); null at the solution root.
    /// </summary>
    public string SolutionFolderPath { get; set; }

    public SolutionProjectModel Model { get; set; }
}

#region Usings
using SF.Models;
using SF.Utils;
using System.Collections.Generic;
using System.IO;
#endregion

namespace SF.Engine;

/// <summary>
/// Writes generated files into a plain folder, one subfolder per project (output/&lt;ProjectName&gt;/...),
/// without project or solution files. Used by GenCheck and the command line's generate.
/// </summary>
public sealed class FolderProjectHost : IProjectHost
{
    private readonly string _outputFolder;

    public int FilesWritten { get; set; }

    public FolderProjectHost(string outputFolder)
    {
        _outputFolder = outputFolder;
    }

    public string PrepareProject(string projectName)
    {
        var path = Path.Combine(_outputFolder, projectName);
        Directory.CreateDirectory(path);
        return path;
    }

    public void PrepareReferences(ModelContainer modelContainer, Model model)
    {
    }

    public void WriteFile(List<CodeFile> filesInModel, string folder, string fileName, string content)
    {
        if(string.IsNullOrWhiteSpace(content))
            return;

        var fullPath = Path.Combine(folder, fileName);
        filesInModel.Add(new CodeFile { FolderPath = folder, FullPath = fullPath, FileName = fileName });
        File.WriteAllText(fullPath, IOUtils.ToCrlf(content));
        FilesWritten++;
    }
}

#region Usings
using SF.Models;
using System.Collections.Generic;
#endregion

namespace SF.Engine;

/// <summary>
/// Where generated projects and files go. Sync writes into a solution; GenCheck writes into a plain folder.
/// </summary>
public interface IProjectHost
{
    /// <summary>
    /// Makes sure the project exists and returns its folder.
    /// </summary>
    string PrepareProject(string projectName);

    /// <summary>
    /// Makes sure the references between the model's projects and the common projects exist.
    /// </summary>
    void PrepareReferences(ModelContainer modelContainer, Model model);

    /// <summary>
    /// Writes one generated file and adds it to <paramref name="filesInModel"/>.
    /// </summary>
    void WriteFile(List<CodeFile> filesInModel, string folder, string fileName, string content);
}

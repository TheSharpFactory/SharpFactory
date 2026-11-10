#region Usings
#endregion

namespace SF.Models;

public sealed class CodeFile
{
    public string FileName { get; set; }
    public string FullPath { get; set; }
    public string FolderPath { get; set; }
    public string ProjectName { get; set; }
}

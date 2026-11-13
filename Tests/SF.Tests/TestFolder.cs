#region Usings
using System;
using System.IO;
#endregion

namespace SF.Tests;

/// <summary>
/// A temporary folder outside the repository, deleted when the test ends. Also used by the app tests (linked).
/// </summary>
internal sealed class TestFolder : IDisposable
{
    public string Path { get; set; }

    public TestFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sharpfactory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Combine(params string[] parts)
    {
        var all = new string[parts.Length + 1];
        all[0] = Path;
        Array.Copy(parts, 0, all, 1, parts.Length);
        return System.IO.Path.Combine(all);
    }

    public void Dispose()
    {
        try
        {
            if(Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
        catch(IOException)
        {
            //a file still open by the test host; the temp folder is cleaned later
        }
    }
}

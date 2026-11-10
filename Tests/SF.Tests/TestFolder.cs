#region Usings
using System;
using System.IO;
using SF.Models;
using SF.Utils;
using Xunit;
#endregion

//generation uses static state (the code builder, the id counter), so tests run one at a time
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SF.Tests;

/// <summary>
/// A temporary folder outside the repository, deleted when the test ends.
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

/// <summary>
/// The public test model: the Coverage schema of the SharpFactoryTest database (synthetic names only).
/// </summary>
internal static class Fixtures
{
    public static string CoverageModelFile
    {
        get
        {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", "Coverage.edmj");
        }
    }

    public static ModelContainer LoadCoverage()
    {
        return ModelUtils.Load(CoverageModelFile);
    }
}

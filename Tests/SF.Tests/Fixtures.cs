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

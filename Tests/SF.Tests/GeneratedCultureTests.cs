#region Usings
using SF.Engine;
using SF.Utils;
using System;
using System.Globalization;
using System.IO;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// The generated code is the same on every machine, whatever its language: names are lowered with the invariant
/// culture, so Turkish (where "I" lowers to a dotless "ı") gives the same files as English. The base classes throw
/// argument exceptions that name their parameter.
/// </summary>
public sealed class GeneratedCultureTests
{
    [Fact]
    public void TurkishGeneratesTheSameFilesAsEnglish()
    {
        using(var folder = new TestFolder())
        {
            var english = folder.Combine("en");
            var turkish = folder.Combine("tr");
            GenerateWith(new CultureInfo("en-US"), english);
            GenerateWith(new CultureInfo("tr-TR"), turkish);

            var files = Directory.GetFiles(english, "*.cs", SearchOption.AllDirectories);
            Assert.NotEmpty(files);
            foreach(var file in files)
            {
                var relative = Path.GetRelativePath(english, file);
                var other = Path.Combine(turkish, relative);
                Assert.True(File.Exists(other), relative);
                Assert.True(File.ReadAllText(file) == File.ReadAllText(other), relative + " differs in Turkish");
            }

            Assert.Equal(files.Length, Directory.GetFiles(turkish, "*.cs", SearchOption.AllDirectories).Length);
            Assert.Contains("public AllType ByPK(int id)", Read(english, "AllTypeRepository.cs"), StringComparison.Ordinal);
            Assert.Contains("a separate file with the following declaration:", Read(english, "AllTypeRepository.cs"), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheBaseClassesNameTheParameterInTheirExceptions()
    {
        using(var folder = new TestFolder())
        {
            var output = folder.Combine("generate");
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);
            var baseClasses = Read(output, "RepositoryBase.cs");

            Assert.Contains("throw new ArgumentNullException(nameof(entities), \"The entities parameter cannot be null. Failed to create list.\");", baseClasses, StringComparison.Ordinal);
            Assert.Contains("throw new ArgumentNullException(nameof(entity), $\"The {typeof(TEntity).FullName} parameter cannot be null. Failed to create.\");", baseClasses, StringComparison.Ordinal);
            Assert.Contains("throw new ArgumentNullException(nameof(batchEntries), \"The batchEntries parameter cannot be null. Failed to update.\");", baseClasses, StringComparison.Ordinal);
            Assert.Contains("throw new ArgumentException(\"The Original property cannot be null. Failed to update.\", nameof(batchEntries));", baseClasses, StringComparison.Ordinal);
            Assert.Contains("throw new InvalidOperationException(\"Failed to create one or more entities.\");", baseClasses, StringComparison.Ordinal);
            Assert.DoesNotContain("NullReferenceException", baseClasses, StringComparison.Ordinal);
            Assert.DoesNotContain("new Exception(", baseClasses, StringComparison.Ordinal);
        }
    }

    private static void GenerateWith(CultureInfo culture, string output)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try
        {
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static string Read(string folder, string name)
    {
        var files = Directory.GetFiles(folder, name, SearchOption.AllDirectories);
        return File.ReadAllText(Assert.Single(files));
    }
}

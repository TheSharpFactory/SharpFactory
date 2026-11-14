#region Usings
using SF.Engine;
using SF.Utils;
using System;
using System.IO;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// What the C# writer (SF.CSharpGenerator) writes: the header's partial-class example names a generic type's type
/// parameters, properties document their value with &lt;value&gt; (fields get no &lt;returns&gt;), and lists and
/// trailing comments are spaced.
/// </summary>
public sealed class CSharpWriterTests
{
    [Fact]
    public void TheWriterFormatsDeclarationsAndComments()
    {
        using(var folder = new TestFolder())
        {
            var output = folder.Combine("generate");
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);

            //the other part of a generic type must repeat its type parameters, or it declares another type
            Assert.Contains("namespace SFTest.Query;\npublic partial class StoredProcResult<TResult>\n{\n}\n", Read(output, "StoredProcResult.cs"), StringComparison.Ordinal);
            Assert.Contains("public partial class BatchEntry<TEntity>\n{\n}\n", Read(output, "BatchEntry.cs"), StringComparison.Ordinal);

            var paging = Read(output, "PaginationSettings.cs");
            Assert.Contains("    /// <value>\n    /// int\n    /// </value>\n    public int PageNumberForRep { get{ return _pageNumberForRep; } }\n", paging, StringComparison.Ordinal);
            Assert.Contains("    public bool GetTotalCount { get{ return _getTotalCount; } set{ _getTotalCount = value; } }\n", paging, StringComparison.Ordinal);
            Assert.Contains("    private int _pageSize = 0; // _pageSize zero means pagination DISABLED.", paging, StringComparison.Ordinal);
            //a field's comment is its summary only (it used to get "<returns> Prop of RegionProperty, string")
            Assert.DoesNotContain("/// Prop of RegionProperty", Read(output, "RegionProperty.cs"), StringComparison.Ordinal);

            Assert.Contains("public partial class RegionRepository : WritableNavRepositoryBase<Region, RegionProperty>, IRegionRepository\n", Read(output, "RegionRepository.cs"), StringComparison.Ordinal);
            Assert.Contains("public sealed class FilterBuilder<TProps, TValue> where TProps : PropertyContainer\n", Read(output, "FilterBuilder.cs"), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Every line the writer indents scales with the indent size: generated with 2 and with 4, each line of every file
    /// has exactly twice the leading spaces with 4 and the same text after them. The SQL engine files are copied as
    /// they are (not written by the writer), so they are left out.
    /// </summary>
    [Fact]
    public void EveryGeneratedLineScalesWithTheIndentSize()
    {
        using(var folder = new TestFolder())
        {
            var four = folder.Combine("four");
            var two = folder.Combine("two");
            GenerateWithIndent(4, four);
            GenerateWithIndent(2, two);

            var checkedLines = 0;
            var indentedLines = 0;
            var files = Directory.GetFiles(four, "*.cs", SearchOption.AllDirectories);
            Assert.NotEmpty(files);
            foreach(var file in files)
            {
                var relative = Path.GetRelativePath(four, file);
                if(relative.Contains("SQLEngine", StringComparison.Ordinal))
                    continue;

                var linesFour = File.ReadAllLines(file);
                var linesTwo = File.ReadAllLines(Path.Combine(two, relative));
                Assert.True(linesFour.Length == linesTwo.Length, relative + " has a different number of lines");
                for(var i = 0; i < linesFour.Length; i++)
                {
                    var spacesFour = CountLeadingSpaces(linesFour[i]);
                    var spacesTwo = CountLeadingSpaces(linesTwo[i]);
                    Assert.True(spacesFour == spacesTwo * 2, $"{relative} line {i + 1}: {spacesFour} spaces with 4, {spacesTwo} with 2");
                    Assert.True(string.Equals(linesFour[i].Substring(spacesFour), linesTwo[i].Substring(spacesTwo), StringComparison.Ordinal), $"{relative} line {i + 1} differs after its indent");
                    checkedLines++;
                    if(spacesFour > 0)
                        indentedLines++;
                }
            }

            Assert.True(checkedLines > 10000, "lines checked: " + checkedLines);
            Assert.True(indentedLines > 5000, "indented lines checked: " + indentedLines);
        }
    }

    /// <summary>
    /// The writer keeps no state between files: generating the same model twice in one process gives the same files.
    /// </summary>
    [Fact]
    public void GeneratingTwiceGivesTheSameFiles()
    {
        using(var folder = new TestFolder())
        {
            var first = folder.Combine("first");
            var second = folder.Combine("second");
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(first), NullProgressLog.Instance);
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(second), NullProgressLog.Instance);

            var files = Directory.GetFiles(first, "*", SearchOption.AllDirectories);
            Assert.NotEmpty(files);
            foreach(var file in files)
            {
                var relative = Path.GetRelativePath(first, file);
                Assert.True(File.ReadAllText(file) == File.ReadAllText(Path.Combine(second, relative)), relative + " differs the second time");
            }

            Assert.Equal(files.Length, Directory.GetFiles(second, "*", SearchOption.AllDirectories).Length);
        }
    }

    private static void GenerateWithIndent(int indentSize, string output)
    {
        var container = Fixtures.LoadCoverage();
        container.CommonSettings.IndentSize = indentSize;
        GenerationRunner.Run(container, new FolderProjectHost(output), NullProgressLog.Instance);
    }

    private static int CountLeadingSpaces(string line)
    {
        var count = 0;
        while(count < line.Length && line[count] == ' ')
            count++;

        return count;
    }

    private static string Read(string folder, string name)
    {
        var files = Directory.GetFiles(folder, name, SearchOption.AllDirectories);
        return File.ReadAllText(Assert.Single(files)).Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

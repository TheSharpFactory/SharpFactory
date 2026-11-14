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

    private static string Read(string folder, string name)
    {
        var files = Directory.GetFiles(folder, name, SearchOption.AllDirectories);
        return File.ReadAllText(Assert.Single(files)).Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

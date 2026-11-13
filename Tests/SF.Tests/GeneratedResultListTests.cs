#region Usings
using SF.Engine;
using SF.Engine.Sync;
using SF.Utils;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// Result lists are created on their first row (an empty result set allocates none), and a to-one navigation property's
/// list is sized by its parent result set's row count.
/// </summary>
public sealed class GeneratedResultListTests
{
    private static readonly Regex _composeCase = new Regex(@"case (?<enum>\w+)\.(?<nav>\w+):\r?\n\s*materializers\[index\] = [\w.]+\.Materialize;\r?\n\s*toOneParents\[index\] = (?<parent>parentIndex|-1);", RegexOptions.Compiled);

    [Fact]
    public void AResultListIsCreatedOnItsFirstRow()
    {
        using(var folder = new TestFolder())
        {
            var text = File.ReadAllText(FindFile(Generate(folder), "RepositoryBase.cs"));
            var method = text.Substring(text.IndexOf("protected static object MaterializeEntityResult<TEntity>(", StringComparison.Ordinal));
            var noRows = method.IndexOf("if(!r.Read())", StringComparison.Ordinal);
            var newList = method.IndexOf("var result = new List<TEntity>(listCapacity > 0 ? listCapacity : _unboundedListCapacity);", StringComparison.Ordinal);

            Assert.True(noRows > 0 && newList > noRows, "the first row is read before the list is created");
            //the first row, then the total row count, then the other rows (the order is checked in GeneratedPagingTests)
            Assert.Matches(new Regex(@"while\(r\.Read\(\)\)\r?\n\s*result\.Add\(materializer\(r\)\);"), method);
            Assert.Contains("capacity = ToOneCapacity(resultSets[toOneParents[resultIndex]]);", text);
            Assert.Contains("protected abstract void GetNavPropMaterializers(NavProps navprops, Func<SqlDataReader, int, object>[] materializers, int[] toOneParents);", text);
        }
    }

    [Fact]
    public void ToOneNavigationPropertiesAreTheOnesThatAreAssigned()
    {
        using(var folder = new TestFolder())
        {
            var toOne = 0;
            var toMany = 0;
            foreach(var file in Directory.GetFiles(Generate(folder), "*Repository.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                var populateAt = text.IndexOf("internal static int PopulateNavProperties(", StringComparison.Ordinal);
                if(populateAt < 0)
                    continue;

                var populate = text.Substring(populateAt);
                foreach(Match m in _composeCase.Matches(text))
                {
                    var region = Regex.Match(populate, @"case " + m.Groups["enum"].Value + @"\." + m.Groups["nav"].Value + @":\s*#region.*?#endregion", RegexOptions.Singleline);
                    Assert.True(region.Success, Path.GetFileName(file) + ": " + m.Groups["nav"].Value);

                    var assigned = !region.Value.Contains("AddEntityToList", StringComparison.Ordinal);
                    var markedToOne = m.Groups["parent"].Value == "parentIndex";
                    Assert.True(assigned == markedToOne, Path.GetFileName(file) + ": " + m.Groups["nav"].Value);
                    if(markedToOne)
                        toOne++;
                    else
                        toMany++;
                }
            }

            Assert.True(toOne > 0, "to-one navigation properties found: " + toOne);
            Assert.True(toMany > 0, "to-many navigation properties found: " + toMany);
        }
    }

    private static string Generate(TestFolder folder)
    {
        var output = folder.Combine("generate");
        GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);
        return output;
    }

    private static string FindFile(string folder, string name)
    {
        var files = Directory.GetFiles(folder, name, SearchOption.AllDirectories);
        Assert.Single(files);
        return files[0];
    }
}

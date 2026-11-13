#region Usings
using SF.Engine;
using SF.Engine.Sync;
using SF.Utils;
using System;
using System.IO;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// Updates from changes: UpdateValuesBuilder.Set(change) takes a change made by GetChanges of an entity's utils or
/// CreateChange of its property container (a property picked by name), the same Set clause as Set(prop, value).
/// GeneratedPagingTests compiles and runs both against the test database.
/// </summary>
public sealed class GeneratedChangesTests
{
    [Fact]
    public void AnUpdateTakesAChangeOfItsOwnEntity()
    {
        using(var folder = new TestFolder())
        {
            var output = folder.Combine("generate");
            GenerationRunner.Run(Fixtures.LoadCoverage(), new FolderProjectHost(output), NullProgressLog.Instance);
            var builder = Read(output, "UpdateValuesBuilder.cs");
            var region = Read(output, "RegionProperty.cs");
            var prop = Read(output, "Prop.cs");

            Assert.Contains("    public UpdateValuesBuilder<TProps> Set(IQueryFilter change)\n", builder, StringComparison.Ordinal);
            Assert.Contains("filter.Owner != typeof(TProps)", builder, StringComparison.Ordinal);
            Assert.Contains("return new PropertyFilter(typeof(TProps), _enumValue, _name,", prop, StringComparison.Ordinal);

            //one case per property, each the typed property's change
            Assert.Contains("    public static IQueryFilter CreateChange(string propertyName, object value)\n", region, StringComparison.Ordinal);
            Assert.Contains("            case nameof(Name):\n                return QueryFilter.New(Name, FilterConditions.Equals, (string)value);\n", region, StringComparison.Ordinal);
            Assert.Contains("throw new ArgumentException($\"{propertyName} is not a property of Region.\", nameof(propertyName));", region, StringComparison.Ordinal);
        }
    }

    private static string Read(string folder, string name)
    {
        var files = Directory.GetFiles(folder, name, SearchOption.AllDirectories);
        return File.ReadAllText(Assert.Single(files)).Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

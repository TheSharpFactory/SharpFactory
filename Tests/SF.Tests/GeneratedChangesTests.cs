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
/// CreateChange of its property container (a property picked at run time, by its id), the same Set clause as Set(prop, value).
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

            Assert.Contains("    public UpdateValuesBuilder<TProps> Set(IQueryFilter<TProps> change)\n    {\n        if(SetValues == null)\n            SetValues = new List<IQueryFilter>();\n        SetValues.Add(change);\n        return this;\n    }", builder, StringComparison.Ordinal);

            //one case per property, each the typed property's change
            //a switch on the property enum: no strings compared
            Assert.Contains("    public static IQueryFilter<RegionProperty> CreateChange(int propertyId, object value)\n", region, StringComparison.Ordinal);
            //typed by the entity: GetChanges and CreateChange give IQueryFilter<RegionProperty>, an update of Region takes only those
            Assert.Contains("public interface IQueryFilter<TProps> : IQueryFilter where TProps : PropertyContainer", Read(output, "IQueryFilter.cs"), StringComparison.Ordinal);
            Assert.Contains("    public static List<IQueryFilter<RegionProperty>> GetChanges(Region original, Region changed)\n", Read(output, "RegionUtils.cs"), StringComparison.Ordinal);
            Assert.Contains("        switch((RegionProp)propertyId)\n", region, StringComparison.Ordinal);
            Assert.Contains("            case RegionProp.Name:\n                return QueryFilter.New(Name, FilterConditions.Equals, (string)value);\n", region, StringComparison.Ordinal);
            Assert.Contains("throw new ArgumentException($\"{propertyId} is not a property of Region.\", nameof(propertyId));", region, StringComparison.Ordinal);
        }
    }

    private static string Read(string folder, string name)
    {
        var files = Directory.GetFiles(folder, name, SearchOption.AllDirectories);
        return File.ReadAllText(Assert.Single(files)).Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

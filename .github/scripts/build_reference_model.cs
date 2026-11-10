#:project ../../Dependencies/SF.Engine/SF.Engine.csproj

// Builds the reference model the database parity tests compare with, from the test database itself, the way the
// app's Add wizard does: every object selected, the default layers, the settings of the given model file. A model
// file only matches a database with the same history (foreign key numbers, object order), so the build machine
// builds its own reference from the database it just created.
//
//   dotnet run --file .github/scripts/build_reference_model.cs -- <settings and connection.edmj> <output.edmj>

using SF.Engine;
using SF.Models;
using SF.Utils;

if(args.Length < 2)
{
    Console.Error.WriteLine("usage: build_reference_model.cs <settings and connection.edmj> <output.edmj>");
    return 2;
}

var source = ModelUtils.Load(args[0]);
var sourceModel = source.Models[0];
var settings = source.CommonSettings;

var categories = ModelBuilder.ReadDatabaseObjects(sourceModel.ConInfo);
foreach(var category in categories)
{
    foreach(var schema in category.Schemas)
    {
        foreach(var dbObject in schema.Objects)
        {
            dbObject.IsSelected = true;
        }
    }
}

var model = ModelBuilder.Populate(sourceModel.Name, sourceModel.ConInfo, Defaults.ModelLayers(), categories, NullProgressLog.Instance);
var container = ModelBuilder.CreateContainer(settings.RootNamespace, settings.DependenciesFolder, settings.IndentSize, settings.UseRootNSInProjectName == YesOrNo.Yes);
container.Models.Add(model);
ModelUtils.Save(container, args[1]);
Console.WriteLine($"Reference model written to {args[1]}");
return 0;

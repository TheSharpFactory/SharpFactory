#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static class RepoContainerInterfaceGenerator
{
    public static string Generate(ModelContainer modelContainer)
    {
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

        var typeName = Common.Names.IRepositoryContainer;
        var typeNamespace = modelContainer.CommonLayers.IRepoContainer.FullNamespace(modelContainer.CommonSettings);
        //var typeDeclaration = $"public partial interface {typeName}";

        var usingNamespaces = new List<UsingNamespace>(1)
        {
        };

        var summary = "This interface represents the Repository Container.";

        var type = new InterfaceDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            Summary = new List<string> { summary }
        };

        //Generate file header with information about the interface.
        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenInterfaceWithNamespace(type);

        foreach(var model in modelContainer.Models)
            PropertyExtensions.InterfaceAuto(new TypeInfo($"{model.Name}.{Common.Names.IModelRepositoryContainer}"), model.Name);

        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();
        return CSharpFile.ToString();
    }
    public static string GenerateModelSpecific(ModelContainer modelContainer, Model model)
    {
        return ModelSpecificContainer.Generate(modelContainer, model);
    }
    public static string GenerateSchemaSpecific(ModelContainer modelContainer, Model model, string schemaName)
    {
        return SchemaSpecificContainer.Generate(modelContainer, model, schemaName);
    }

    private static class ModelSpecificContainer
    {
        public static string Generate(ModelContainer modelContainer, Model model)
        {
            var entCat = model.Categories.GetByName(CategoryNames.Entities);
            var viewCat = model.Categories.GetByName(CategoryNames.Views);
            var procCat = model.Categories.GetByName(CategoryNames.Procs);
            var funcCat = model.Categories.GetByName(CategoryNames.Funcs);
            var seqCat = model.Categories.GetByName(CategoryNames.Seqs);

            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeName = Common.Names.IModelRepositoryContainer;
            var typeNamespace = modelContainer.CommonLayers.IRepoContainer.FullNamespace(modelContainer.CommonSettings) + $".{model.Name}";
            //var typeDeclaration = $"public partial interface {typeName}";
            var usingNamespaces = new List<UsingNamespace>(1)
            {
            };
            var summary = "This interface represents the Repository Container for a specific Model.";

            var type = new InterfaceDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string> { summary }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenInterfaceWithNamespace(type);

            var schemaNames = new HashSet<string>();
            AddSchemasToProcess(entCat, schemaNames);
            AddSchemasToProcess(viewCat, schemaNames);
            AddSchemasToProcess(procCat, schemaNames);
            AddSchemasToProcess(funcCat, schemaNames);
            AddSchemasToProcess(seqCat, schemaNames);

            foreach(var schema in schemaNames)
                PropertyExtensions.InterfaceAuto(new TypeInfo($"{schema}.{Common.Names.ISchemaRepositoryContainer}"), schema);

            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
        private static void AddSchemasToProcess(Category cat, HashSet<string> schemas)
        {
            if(cat.Count < 1)
                return;

            foreach(var s in cat.Schemas)
            {
                if(s.Count < 1)
                    continue;
                if(schemas.Contains(s.Name))
                    continue;
                schemas.Add(s.Name);
            }
        }
    }

    private static class SchemaSpecificContainer
    {
        public static string Generate(ModelContainer modelContainer, Model model, string schemaName)
        {
            var entCat = model.Categories.GetByName(CategoryNames.Entities);
            var viewCat = model.Categories.GetByName(CategoryNames.Views);
            var procCat = model.Categories.GetByName(CategoryNames.Procs);
            var funcCat = model.Categories.GetByName(CategoryNames.Funcs);
            var seqCat = model.Categories.GetByName(CategoryNames.Seqs);

            var entSchema = entCat.Schemas.Contains(schemaName) ? entCat.Schemas.GetByName(schemaName) : null;
            var viewSchema = viewCat.Schemas.Contains(schemaName) ? viewCat.Schemas.GetByName(schemaName) : null;
            var procSchema = procCat.Schemas.Contains(schemaName) ? procCat.Schemas.GetByName(schemaName) : null;
            var funcSchema = funcCat.Schemas.Contains(schemaName) ? funcCat.Schemas.GetByName(schemaName) : null;
            var seqSchema = seqCat.Schemas.Contains(schemaName) ? seqCat.Schemas.GetByName(schemaName) : null;

            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeName = Common.Names.ISchemaRepositoryContainer;
            var typeNamespace = $"{modelContainer.CommonLayers.IRepoContainer.FullNamespace(modelContainer.CommonSettings)}.{model.Name}.{schemaName}";
            var iRepoNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schemaName);
            //var typeDeclaration = $"public partial interface {typeName}";

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = iRepoNamespace }
            };

            var summary = "This interface represents the Repository Container for a specific Schema within the Model.";

            var type = new InterfaceDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string> { summary }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenInterfaceWithNamespace(type);
            if(entSchema?.Count > 0)
            {
                CSharpFile.OpenRegion("Entity Repositories");
                foreach(var ent in entSchema.Entities)
                    PropertyExtensions.InterfaceAuto(new TypeInfo(Common.GenerateIRepositoryName(ent)), ent.N);

                CSharpFile.CloseRegion();
            }

            if(viewSchema?.Count > 0)
            {
                CSharpFile.OpenRegion("View Repositories");
                foreach(var ent in viewSchema.Views)
                    PropertyExtensions.InterfaceAuto(new TypeInfo(Common.GenerateIRepositoryName(ent)), ent.N);

                CSharpFile.CloseRegion();
            }

            if(procSchema?.Count > 0)
            {
                CSharpFile.OpenRegion("Store Procedure Repository");
                PropertyExtensions.InterfaceAuto(new TypeInfo(Common.Names.IStoredProcRepository), "StoredProcs");

                CSharpFile.CloseRegion();
            }

            if(funcSchema?.Count > 0)
            {
                CSharpFile.OpenRegion("Function Repository");
                PropertyExtensions.InterfaceAuto(new TypeInfo(Common.Names.IFunctionRepository), "Functions");

                CSharpFile.CloseRegion();
            }

            if(seqSchema?.Count > 0)
            {
                CSharpFile.OpenRegion("Sequence Repository");
                PropertyExtensions.InterfaceAuto(new TypeInfo(Common.Names.ISequenceRepository), "Sequences");

                CSharpFile.CloseRegion();
            }

            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
}

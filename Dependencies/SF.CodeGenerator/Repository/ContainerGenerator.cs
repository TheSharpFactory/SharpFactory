#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static class RepoContainerGenerator
{
    public static string Generate(ModelContainer modelContainer)
    {
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

        var typeName = Common.Names.RepositoryContainer;
        var typeNamespace = modelContainer.CommonLayers.RepoContainer.FullNamespace(modelContainer.CommonSettings);
        var iNamespaceAliasName = $"IContainer";
        var iNamespaceAlias = $"{iNamespaceAliasName} = {modelContainer.CommonLayers.IRepoContainer.FullNamespace(modelContainer.CommonSettings)}";
        //var typeDeclaration = $"public partial class {typeName}";

        var usingNamespaces = new List<UsingNamespace>(1)
        {
           new UsingNamespace { Name = iNamespaceAlias },
        };
        var summary = $"This class contains all Repository Containers for all Models.";

        var type = new ClassDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            Interfaces = new List<string> { $"{iNamespaceAliasName}.{Common.Names.IRepositoryContainer}" },
            Summary = new List<string> { summary }
        };

        //Generate file header with information about the interface.
        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenClassWithNamespace(type);

        foreach(var model in modelContainer.Models)
            PropertyExtensions.Auto(new TypeInfo($"{iNamespaceAliasName}.{model.Name}.{Common.Names.IModelRepositoryContainer}"), model.Name);

        CSharpFile.Constructor(new ConstructorDeclaration { Name = typeName });
        CSharpFile.OpenBlock();

        foreach(var model in modelContainer.Models)
            CSharpFile.Line($"{model.Name} = new {model.Name}.{Common.Names.ModelRepositoryContainer}()");

        CSharpFile.Comment("If not implemented this method call is removed during compilation");
        CSharpFile.Line("LoadCustomContainers()");

        CSharpFile.CloseBlock();

        var method = new MethodDeclaration
        {
            Name = "LoadCustomContainers",
            AccessModifier = AccessModifiers.Private,
            IsPartial = true,
            Summary = new List<string> { "Implement this method in a partial class to load custom containers" }
        };

        CSharpFile.DeclareMethod(method);
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

            var schemaNames = new HashSet<string>();

            AddSchemasToProcess(entCat, schemaNames);
            AddSchemasToProcess(viewCat, schemaNames);
            AddSchemasToProcess(procCat, schemaNames);
            AddSchemasToProcess(funcCat, schemaNames);
            AddSchemasToProcess(seqCat, schemaNames);

            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeName = Common.Names.ModelRepositoryContainer;
            var typeNamespace = modelContainer.CommonLayers.RepoContainer.FullNamespace(modelContainer.CommonSettings) + $".{model.Name}";
            var interfaceTypeNamespace = modelContainer.CommonLayers.IRepoContainer.FullNamespace(modelContainer.CommonSettings) + $".{model.Name}";
            //var typeDeclaration = $"public partial class {typeName}";
            var iNamespaceAliasName = $"I{model.Name}";
            var iNamespaceAlias = $"{iNamespaceAliasName} = {interfaceTypeNamespace}";
            var usingNamespaces = new List<UsingNamespace>(1)
            {
               new UsingNamespace { Name = iNamespaceAlias },
            };

            var summary = "This class represents the Repository Container for a specific Model.";

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                IsPartial = true,
                Interfaces = new List<string> { $"{iNamespaceAliasName}.{Common.Names.IModelRepositoryContainer}" },
                Summary = new List<string> { summary }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            foreach(var schema in schemaNames)
                PropertyExtensions.Auto(new TypeInfo($"{iNamespaceAliasName}.{schema}.{Common.Names.ISchemaRepositoryContainer}"), schema);

            CSharpFile.Constructor(new ConstructorDeclaration { Name = typeName });
            CSharpFile.OpenBlock();

            foreach(var schema in schemaNames)
                CSharpFile.Line($"{schema} = new {schema}.{Common.Names.SchemaRepositoryContainer}()");

            CSharpFile.Comment("If not implemented this method call is removed during compilation");
            CSharpFile.Line("LoadCustomContainers()");

            CSharpFile.CloseBlock();

            var method = new MethodDeclaration
            {
                Name = "LoadCustomContainers",
                AccessModifier = AccessModifiers.Private,
                IsPartial = true,
                Summary = new List<string> { "Implement this method in a partial class to load custom containers" }
            };

            CSharpFile.DeclareMethod(method);

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
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var schemas = GetSchemas(model, schemaName);

            var typeName = Common.Names.SchemaRepositoryContainer;
            var typeNamespace = $"{modelContainer.CommonLayers.RepoContainer.FullNamespace(modelContainer.CommonSettings)}.{model.Name}.{schemaName}";
            var iRepoNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schemaName);
            var repoNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Repository, model, schemaName);
            var iContainerNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, modelContainer.CommonLayers.IRepoContainer, model, schemaName);
            //var typeDeclaration = $"public partial class {typeName}";

            var usingNamespaces = new List<UsingNamespace>(4)
            {
              new UsingNamespace { Name = repoNamespace },
              new UsingNamespace { Name = iRepoNamespace },
              new UsingNamespace { Name = iContainerNamespace }
            };

            //using VLM.Repository.AppDB.Finance;
            //using VLM.Repository.Interfaces.AppDB.Finance;
            //using VLM.Repository.Container.Interfaces.AppDB.Finance;
            var summary = "This class represents the Repository Container for a specific Schema within the Model.";

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                IsPartial = true,
                Interfaces = new List<string> { Common.Names.ISchemaRepositoryContainer },
                Summary = new List<string> { summary }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);

            CSharpFile.OpenClassWithNamespace(type);

            GenerateEntityIContainerProperties(schemas);
            GenerateViewIContainerProperties(schemas);
            GenerateStoreProcIContainerProperties(schemas);
            GenerateFuncIContainerProperties(schemas);
            GenerateSeqIContainerProperties(schemas);

            CSharpFile.Constructor(new ConstructorDeclaration { Name = typeName });
            CSharpFile.OpenBlock();

            GenerateEntityContainerProperties(schemas);
            GenerateViewContainerProperties(schemas);
            GenerateStoreProcContainerProperties(schemas);
            GenerateFuncContainerProperties(schemas);
            GenerateSeqContainerProperties(schemas);

            CSharpFile.Comment("If not implemented this method call is removed during compilation");
            CSharpFile.Line("LoadCustomRepositories()");

            CSharpFile.CloseBlock();

            var method = new MethodDeclaration
            {
                Name = "LoadCustomRepositories",
                AccessModifier = AccessModifiers.Private,
                IsPartial = true,
                Summary = new List<string> { "Implement this method in a partial class to load custom repositories" }
            };
            CSharpFile.DeclareMethod(method);
            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }

        private static void GenerateEntityIContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.EntSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("Entity Repositories");

            foreach(var ent in schemas.EntSchema.Entities)
                PropertyExtensions.Auto(new TypeInfo(Common.GenerateIRepositoryName(ent)), ent.N);

            CSharpFile.CloseRegion();
        }
        private static void GenerateViewIContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.VieSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("View Repositories");

            foreach(var ent in schemas.VieSchema.Views)
                PropertyExtensions.Auto(new TypeInfo(Common.GenerateIRepositoryName(ent)), ent.N);

            CSharpFile.CloseRegion();
        }
        private static void GenerateStoreProcIContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.SPSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("Store Procedure Repository");
            PropertyExtensions.Auto(new TypeInfo(Common.Names.IStoredProcRepository), "StoredProcs");

            CSharpFile.CloseRegion();
        }
        private static void GenerateFuncIContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.FunSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("Function Repository");
            PropertyExtensions.Auto(new TypeInfo(Common.Names.IFunctionRepository), "Functions");
            CSharpFile.CloseRegion();
        }
        private static void GenerateSeqIContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.SeqSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("Sequence Repository");
            PropertyExtensions.Auto(new TypeInfo(Common.Names.ISequenceRepository), "Sequences");
            CSharpFile.CloseRegion();
        }

        private static void GenerateEntityContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.EntSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("Entity Repositories");

            foreach(var ent in schemas.EntSchema.Entities)
                CSharpFile.Line($"{ent.N} = new {Common.GenerateRepositoryName(ent)}()");
            CSharpFile.CloseRegion();
        }
        private static void GenerateViewContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.VieSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("View Repositories");

            foreach(var ent in schemas.VieSchema.Views)
                CSharpFile.Line($"{ent.N} = new {Common.GenerateRepositoryName(ent)}()");
            CSharpFile.CloseRegion();
        }
        private static void GenerateStoreProcContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.SPSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("Store Procedure Repository");
            CSharpFile.Line($"StoredProcs = new {Common.Names.StoredProcRepository}()");
            CSharpFile.CloseRegion();
        }
        private static void GenerateFuncContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.FunSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("Function Repository");
            CSharpFile.Line($"Functions = new {Common.Names.FunctionRepository}()");
            CSharpFile.CloseRegion();
        }
        private static void GenerateSeqContainerProperties(SchemaContainer schemas)
        {
            if(!(schemas.SeqSchema?.Count > 0))
                return;

            CSharpFile.OpenRegion("Sequence Repository");
            CSharpFile.Line($"Sequences = new {Common.Names.SequenceRepository}()");
            CSharpFile.CloseRegion();
        }

        private static SchemaContainer GetSchemas(Model model, string schemaName)
        {
            var entCat = model.Categories.GetByName(CategoryNames.Entities);
            var viewCat = model.Categories.GetByName(CategoryNames.Views);
            var procCat = model.Categories.GetByName(CategoryNames.Procs);
            var funcCat = model.Categories.GetByName(CategoryNames.Funcs);
            var seqCat = model.Categories.GetByName(CategoryNames.Seqs);

            var result = new SchemaContainer
            {
                EntSchema = entCat.Schemas.Contains(schemaName) ? entCat.Schemas.GetByName(schemaName) : null,
                VieSchema = viewCat.Schemas.Contains(schemaName) ? viewCat.Schemas.GetByName(schemaName) : null,
                SPSchema = procCat.Schemas.Contains(schemaName) ? procCat.Schemas.GetByName(schemaName) : null,
                FunSchema = funcCat.Schemas.Contains(schemaName) ? funcCat.Schemas.GetByName(schemaName) : null,
                SeqSchema = seqCat.Schemas.Contains(schemaName) ? seqCat.Schemas.GetByName(schemaName) : null,
            };
            return result;
        }
        private sealed class SchemaContainer
        {
            public Schema EntSchema { get; set; }
            public Schema VieSchema { get; set; }
            public Schema SPSchema { get; set; }
            public Schema FunSchema { get; set; }
            public Schema SeqSchema { get; set; }
        }
    }
}

#region Usings
using SF.CodeGeneration;
using SF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace SF.Utils;

public static class CodeGenUtils
{
    /// <summary>
    /// Writes one generated file (files list, folder, file name, content). The engine's project hosts set it for
    /// each run (Sync, GenCheck, the code preview); the default writes the file when its content changed.
    /// </summary>
    public static Action<List<CodeFile>, string, string, string> FileWriter { get; set; } = WriteIfChanged;

    /// <summary>
    /// Adds the file to the list and writes it (CRLF) when the content on disk is different.
    /// </summary>
    public static void WriteIfChanged(List<CodeFile> filesInModel, string folder, string fileName, string content)
    {
        if(string.IsNullOrWhiteSpace(content))
            return;

        var fullPath = System.IO.Path.Combine(folder, fileName);
        filesInModel.Add(new CodeFile { FolderPath = folder, FullPath = fullPath, FileName = fileName });

        var text = IOUtils.ToCrlf(content);
        if(IOUtils.IsFileContentDifferent(fullPath, text))
            System.IO.File.WriteAllText(fullPath, text);
    }

    public static void Reset(CommonSettings commonSettings)
    {
        IdGenerator.Reset();
    }

    public static class ModelProjects
    {
        public static void ProcessEntity(List<CodeFile> filesInModel, CommonSettings commonSettings, Model model, Entity entity, Schema schema, string folder)
        {
            var content = EntityGenerator.Generate(commonSettings, model, entity, schema);
            FileWriter(filesInModel, folder, CodeGeneration.Common.GenerateTypeName(entity) + ".cs", content);
        }
        public static void ProcessStoredProcEntity(List<CodeFile> filesInModel, CommonSettings commonSettings, Model model, StoredProc proc, Schema schema, string folder)
        {
            if(!(proc.Par?.Count > 0) && proc.En == null)
                return;

            var content = EntityGenerator.GenerateStoredProc(commonSettings, model, proc, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "StoredProcs"), CodeGeneration.Common.GenerateTypeName(proc.N) + ".cs", content);
        }
        public static void ProcessFunctionEntity(List<CodeFile> filesInModel, CommonSettings commonSettings, Model model, Function func, Schema schema, string folder)
        {
            if(!(func.Par?.Count > 0) && func.En == null)
                return;

            var content = EntityGenerator.GenerateFunction(commonSettings, model, func, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "Functions"), CodeGeneration.Common.GenerateTypeName(func.N) + ".cs", content);
        }
        public static void ProcessPropertyEnums(List<CodeFile> filesInModel, CommonSettings commonSettings, Model model, Entity entity, Schema schema, string folder)
        {
            var content = EnumsGenerator.GenerateEntityPropertyEnum(commonSettings, model, entity, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "PropertyEnums"), CodeGeneration.Common.GeneratePropEnumName(entity) + ".cs", content);
        }
        public static void ProcessStoredProcPropertyEnums(List<CodeFile> filesInModel, CommonSettings commonSettings, Model model, StoredProc entity, Schema schema, string folder)
        {
            var content = EnumsGenerator.GenerateStoredProcPropertyEnum(commonSettings, model, entity, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "StoredProcs"), CodeGeneration.Common.GenerateRequestTypeName(entity.N) + ".cs", content);
        }
        public static void ProcessNavPropertyEnums(List<CodeFile> filesInModel, CommonSettings commonSettings, Model model, Entity entity, Schema schema, string folder)
        {
            var content = EnumsGenerator.GenerateNavPropertyEnum(commonSettings, model, entity, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "NavPropertyEnums"), CodeGeneration.Common.GenerateNavPropEnumName(entity) + ".cs", content);
        }
        public static void ProcessEntityUtils(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Entity entity, Schema schema, string folder)
        {
            var content = EntityUtilGenerator.Generate(modelContainer, model, entity, schema);
            FileWriter(filesInModel, folder, CodeGeneration.Common.GenerateEntityUtilName(entity) + ".cs", content);
        }
        public static void ProcessIRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Entity entity, Schema schema, string folder, bool isReadOnly)
        {
            var content = RepoInterfaceGenerator.Generate(modelContainer, model, entity, schema, isReadOnly);
            FileWriter(filesInModel, folder, CodeGeneration.Common.GenerateIRepositoryName(entity) + ".cs", content);
        }
        public static void ProcessRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Entity entity, Schema schema, string folder, bool isReadOnly)
        {
            var content = RepoGenerator.Generate(modelContainer, model, entity, schema, isReadOnly);
            FileWriter(filesInModel, folder, CodeGeneration.Common.GenerateRepositoryName(entity) + ".cs", content);
        }
        public static void ProcessIStoredProcRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Schema schema, string folder)
        {
            var content = RepoInterfaceGenerator.GenerateStoredProcs(modelContainer, model, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "StoredProcs"), CodeGeneration.Common.Names.IStoredProcRepository + ".cs", content);
        }
        public static void ProcessStoredProcRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Schema schema, string folder)
        {
            var content = RepoGenerator.GenerateStoredProcs(modelContainer, model, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "StoredProcs"), CodeGeneration.Common.Names.StoredProcRepository + ".cs", content);
        }
        public static void ProcessIFunctionRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Schema schema, string folder)
        {
            var content = RepoInterfaceGenerator.GenerateFunctions(modelContainer, model, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "Functions"), CodeGeneration.Common.Names.IFunctionRepository + ".cs", content);
        }
        public static void ProcessFunctionRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Schema schema, string folder)
        {
            var content = RepoGenerator.GenerateFunctions(modelContainer, model, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "Functions"), CodeGeneration.Common.Names.FunctionRepository + ".cs", content);
        }
        public static void ProcessISequenceRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Schema schema, string folder)
        {
            var content = RepoInterfaceGenerator.GenerateSequences(modelContainer, model, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "Sequences"), CodeGeneration.Common.Names.ISequenceRepository + ".cs", content);
        }
        public static void ProcessSequenceRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Schema schema, string folder)
        {
            var content = RepoGenerator.GenerateSequences(modelContainer, model, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "Sequences"), CodeGeneration.Common.Names.SequenceRepository + ".cs", content);
        }
    }
    public static class CommonProjects
    {
        public static void ProcessQuery(List<CodeFile> filesInModel, ModelContainer modelContainer, string folder)
        {
            var subFolder = IOUtils.CreateSubFolder(folder, "Entities");

            var content = CommonEntityGenerator.GenerateStoredProcResult(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.StoredProcResult + ".cs", content);

            content = CommonEntityGenerator.GenerateBatchEntry(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.BatchEntry + ".cs", content);

            //--------------------------------------------------------------------

            subFolder = IOUtils.CreateSubFolder(folder, "Query");

            content = QueryGenerator.GenerateQueryClass(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.Query + ".cs", content);

            content = QueryGenerator.GenerateQueryWrapper(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.QueryWrapper + ".cs", content);

            //--------------------------------------------------------------------

            subFolder = IOUtils.CreateSubFolder(folder, @"Query\Props");

            content = QueryGenerator.GeneratePropertyContainer(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.PropertyContainer + ".cs", content);

            content = QueryGenerator.GeneratePropertyFilter(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.PropertyFilter + ".cs", content);

            content = QueryGenerator.GeneratePropClass(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.Prop + ".cs", content);

            content = QueryGenerator.GenerateFilterBuilderExtensions(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.FilterBuilderExtensions + ".cs", content);

            //--------------------------------------------------------------------

            subFolder = IOUtils.CreateSubFolder(folder, @"Query\Builders");

            content = QueryGenerator.GenerateQueryBuilder(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.QueryBuilder + ".cs", content);

            content = QueryGenerator.GeneratePredicateBuilder(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.PredicateBuilder + ".cs", content);

            content = QueryGenerator.GeneratePaginationBuilder(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.PaginationBuilder + ".cs", content);

            content = QueryGenerator.GenerateExpressionBuilder(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.ExpressionBuilder + ".cs", content);

            content = QueryGenerator.GenerateFilterBuilder(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.FilterBuilder + ".cs", content);

            content = QueryGenerator.GenerateSortBuilder(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.SortBuilder + ".cs", content);

            content = QueryGenerator.GenerateUpdateValuesBuilder(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.UpdateValuesBuilder + ".cs", content);

            content = QueryGenerator.GenerateNavPropsBuilder(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.NavPropsBuilder + ".cs", content);

            //--------------------------------------------------------------------

            subFolder = IOUtils.CreateSubFolder(folder, @"Query\Filters");

            content = QueryGenerator.GeneratePaginationSettings(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.PaginationSettings + ".cs", content);

            content = QueryGenerator.GenerateQueryFilter(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.QueryFilter + ".cs", content);

            content = QueryGenerator.GenerateSortFilter(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.SortFilter + ".cs", content);

            //--------------------------------------------------------------------

            subFolder = IOUtils.CreateSubFolder(folder, @"Query\Filters\Comparison");

            content = QueryGenerator.GenerateComparers(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.Compare + ".cs", content);

            //--------------------------------------------------------------------

            subFolder = IOUtils.CreateSubFolder(folder, @"Query\Filters\Enums");

            content = QueryGenerator.GenerateFilterConditionsEnum(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.FilterConditionsEnum + ".cs", content);

            content = QueryGenerator.GenerateOperatorsEnum(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.FilterOperatorsEnum + ".cs", content);

            content = QueryGenerator.GenerateSortDirectionEnum(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.SortDirectionEnum + ".cs", content);

            //--------------------------------------------------------------------

            subFolder = IOUtils.CreateSubFolder(folder, @"Query\Interfaces");

            content = QueryGenerator.GeneratePaginationSettingsInterface(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.IPaginationSettings + ".cs", content);

            content = QueryGenerator.GenerateQueryFilterInterface(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.IQueryFilter + ".cs", content);

            content = QueryGenerator.GenerateSortFilterInterface(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.ISortFilter + ".cs", content);

            //--------------------------------------------------------------------

            subFolder = IOUtils.CreateSubFolder(folder, @"Query\NavProps");

            content = QueryGenerator.GenerateNavProp(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.NavProp + ".cs", content);

            content = QueryGenerator.GenerateNavPropCollection(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.NavPropCollection + ".cs", content);

            content = QueryGenerator.GenerateNavPropOfContainer(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.NavPropOfContainer + ".cs", content);

            content = QueryGenerator.GenerateNavPropQueryExtensions(modelContainer);
            FileWriter(filesInModel, subFolder, CodeGeneration.Common.Names.NavPropQueryExtensions + ".cs", content);

            //--------------------------------------------------------------------

            //the containers of every table, view and stored procedure: Props\<model>\<schema>\<Entity>Property.cs
            foreach(var model in modelContainer.Models)
            {
                ProcessEntityProps(filesInModel, modelContainer, model, model.Categories.GetByName(CategoryNames.Entities), folder);
                ProcessEntityProps(filesInModel, modelContainer, model, model.Categories.GetByName(CategoryNames.Views), folder);
                ProcessStoredProcProps(filesInModel, modelContainer, model, model.Categories.GetByName(CategoryNames.Procs), folder);
            }
        }
        private static void ProcessStoredProcProps(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Category category, string folder)
        {
            if(category == null)
                return;

            foreach(var schema in category.Schemas)
            {
                if(!(schema.StoredProcs?.Count > 0))
                    continue;

                var schemaFolder = IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(folder, "Props"), model.Name), schema.Name);
                foreach(var proc in schema.StoredProcs)
                {
                    var content = QueryGenerator.GenerateStoredProcProps(modelContainer, model, schema, proc);
                    if(content != null)
                        FileWriter(filesInModel, schemaFolder, CodeGeneration.Common.GeneratePropClassName(proc) + ".cs", content);
                }
            }
        }
        private static void ProcessEntityProps(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, Category category, string folder)
        {
            if(category == null)
                return;

            foreach(var schema in category.Schemas)
            {
                var entities = schema.Type == ModelObjectType.Views ? schema.Views : schema.Entities;
                if(!(entities?.Count > 0))
                    continue;

                var schemaFolder = IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(folder, "Props"), model.Name), schema.Name);
                foreach(var entity in entities)
                {
                    var content = QueryGenerator.GenerateEntityProps(modelContainer, model, schema, entity);
                    if(content != null)
                        FileWriter(filesInModel, schemaFolder, CodeGeneration.Common.GeneratePropClassName(entity) + ".cs", content);
                }
            }
        }
        public static void ProcessExceptions(List<CodeFile> filesInModel, ModelContainer modelContainer, string folder)
        {
            var content = CommonEntityGenerator.GenerateExceptionClass(modelContainer);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "Exceptions"), CodeGeneration.Common.Names.DataExchangeException + ".cs", content);
        }
        public static void ProcessRepository(List<CodeFile> filesInModel, ModelContainer modelContainer, string folder)
        {
            var content = RepoGenerator.GenerateLookupClass(modelContainer);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "Lookups"), CodeGeneration.Common.Names.RepositoryLookup + ".cs", content);

            content = RepoGenerator.GenerateEntityInfoClass(modelContainer);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "EntityInfo"), CodeGeneration.Common.Names.RepositoryEntityInfo + ".cs", content);

            content = RepoGenerator.GenerateMultiKeyDictionary(modelContainer);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "Dictionaries"), CodeGeneration.Common.Names.MultiKeyDictionary + ".cs", content);

            content = RepoGenerator.GenerateBaseClass(modelContainer);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "BaseClasses"), CodeGeneration.Common.Names.RepositoryBase + ".cs", content);

            var contents = RepoGenerator.GenerateSQLEngine(modelContainer);
            var fileNames = contents.Keys.ToList();
            var count = fileNames.Count;

            for(var i = 0; i < count; i++)
            {
                var fileName = fileNames[i];

                FileWriter(filesInModel, IOUtils.CreateSubFolder(folder, "SQLEngine"), fileName, contents[fileName]);
            }
        }
        public static void ProcessIRepositoryContainer(List<CodeFile> filesInModel, ModelContainer modelContainer, string folder)
        {
            var content = RepoContainerInterfaceGenerator.Generate(modelContainer);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(folder, "Container"), "Common"), CodeGeneration.Common.Names.IRepositoryContainer + ".cs", content);
        }
        public static void ProcessRepositoryContainer(List<CodeFile> filesInModel, ModelContainer modelContainer, string folder)
        {
            var content = RepoContainerGenerator.Generate(modelContainer);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(folder, "Container"), "Common"), CodeGeneration.Common.Names.RepositoryContainer + ".cs", content);
        }
        public static void ProcessModelIRepositoryContainer(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, string folder)
        {
            var content = RepoContainerInterfaceGenerator.GenerateModelSpecific(modelContainer, model);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(folder, "Container"), model.Name), CodeGeneration.Common.Names.IModelRepositoryContainer + ".cs", content);
        }
        public static void ProcessModelRepositoryContainer(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, string folder)
        {
            var content = RepoContainerGenerator.GenerateModelSpecific(modelContainer, model);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(folder, "Container"), model.Name), CodeGeneration.Common.Names.ModelRepositoryContainer + ".cs", content);
        }
        public static void ProcessSchemaIRepositoryContainer(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, string schema, string folder)
        {
            var content = RepoContainerInterfaceGenerator.GenerateSchemaSpecific(modelContainer, model, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(folder, "Container"), model.Name), schema), CodeGeneration.Common.Names.ISchemaRepositoryContainer + ".cs", content);
        }
        public static void ProcessSchemaRepositoryContainer(List<CodeFile> filesInModel, ModelContainer modelContainer, Model model, string schema, string folder)
        {
            var content = RepoContainerGenerator.GenerateSchemaSpecific(modelContainer, model, schema);
            FileWriter(filesInModel, IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(IOUtils.CreateSubFolder(folder, "Container"), model.Name), schema), CodeGeneration.Common.Names.SchemaRepositoryContainer + ".cs", content);
        }
    }
}

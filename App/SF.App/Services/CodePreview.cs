#region Usings
using SF.Engine;
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace SF.App.Services;

/// <summary>
/// One generated file shown in the code preview.
/// </summary>
internal sealed class CodeFileViewModel
{
    public string FileName { get; set; }
    public string Content { get; set; }
}

/// <summary>
/// Generates, in memory, the files of one table, view, procedure or function, for the Models page's code
/// preview. Nothing is written to disk. Uses the generator's shared state, so it must not run during a Sync.
/// </summary>
internal static class CodePreview
{
    public static List<CodeFileViewModel> ForObject(ModelContainer container, Model model, Schema schema, object item)
    {
        //the generation uses a copy when the model has a sql_variant column: find the same objects in it
        var filtered = SqlVariantFilter.Apply(container, NullProgressLog.Instance);
        if(!object.ReferenceEquals(filtered, container))
        {
            container = filtered;
            var copiedModel = container.Models.GetByName(model.Name);
            var copiedSchema = FindSchema(copiedModel, schema);
            var copiedItem = FindItem(copiedSchema, schema.Type, item);
            if(copiedItem == null)
                return new List<CodeFileViewModel>();

            model = copiedModel;
            schema = copiedSchema;
            item = copiedItem;
        }

        var writer = new CapturingWriter();
        var previousWriter = CodeGenUtils.FileWriter;
        var previousCreateFolders = IOUtils.CreateFolders;
        var folder = Path.Combine(Path.GetTempPath(), "SharpFactoryPreview");
        var files = new List<CodeFile>();

        IOUtils.CreateFolders = false;
        try
        {
            CodeGenUtils.FileWriter = writer.Write;
            CodeGenUtils.Reset(container.CommonSettings);

            if(item is Entity entity)
            {
                var isView = schema.Type == ModelObjectType.Views;
                CodeGenUtils.ModelProjects.ProcessEntity(files, container.CommonSettings, model, entity, schema, folder);
                CodeGenUtils.ModelProjects.ProcessPropertyEnums(files, container.CommonSettings, model, entity, schema, folder);
                CodeGenUtils.ModelProjects.ProcessNavPropertyEnums(files, container.CommonSettings, model, entity, schema, folder);
                CodeGenUtils.ModelProjects.ProcessEntityUtils(files, container, model, entity, schema, folder);
                CodeGenUtils.ModelProjects.ProcessIRepository(files, container, model, entity, schema, folder, isView);
                CodeGenUtils.ModelProjects.ProcessRepository(files, container, model, entity, schema, folder, isView);

                //the typed properties, which the Query project holds
                var props = CodeGeneration.QueryGenerator.GenerateEntityProps(container, model, schema, entity);
                if(props != null)
                    writer.Write(files, folder, CodeGeneration.Common.GeneratePropClassName(entity) + ".cs", props);
            }
            else if(item is StoredProc proc)
            {
                CodeGenUtils.ModelProjects.ProcessStoredProcEntity(files, container.CommonSettings, model, proc, schema, folder);
                CodeGenUtils.ModelProjects.ProcessStoredProcPropertyEnums(files, container.CommonSettings, model, proc, schema, folder);

                //the parameter container, which the Query project holds
                var props = CodeGeneration.QueryGenerator.GenerateStoredProcProps(container, model, schema, proc);
                if(props != null)
                    writer.Write(files, folder, CodeGeneration.Common.GeneratePropClassName(proc) + ".cs", props);
            }
            else if(item is Function func)
            {
                CodeGenUtils.ModelProjects.ProcessFunctionEntity(files, container.CommonSettings, model, func, schema, folder);
            }
        }
        finally
        {
            IOUtils.CreateFolders = previousCreateFolders;
            CodeGenUtils.FileWriter = previousWriter;
        }

        return writer.Files;
    }

    private static Schema FindSchema(Model model, Schema original)
    {
        var categoryName = CategoryName(original.Type);
        var category = categoryName == null ? null : model.Categories.GetByName(categoryName);
        if(category?.Schemas == null)
            return null;

        foreach(var schema in category.Schemas)
        {
            if(string.Equals(schema.Name, original.Name, StringComparison.Ordinal))
                return schema;
        }

        return null;
    }

    private static string CategoryName(ModelObjectType type)
    {
        switch(type)
        {
            case ModelObjectType.Entities:
                return CategoryNames.Entities;
            case ModelObjectType.Views:
                return CategoryNames.Views;
            case ModelObjectType.StoredProcs:
                return CategoryNames.Procs;
            case ModelObjectType.Functions:
                return CategoryNames.Funcs;
        }

        return null;
    }

    private static object FindItem(Schema schema, ModelObjectType type, object original)
    {
        if(schema == null)
            return null;

        if(original is Entity entity)
        {
            var entities = type == ModelObjectType.Views ? schema.Views : schema.Entities;
            if(entities == null)
                return null;

            foreach(var candidate in entities)
            {
                if(string.Equals(candidate.SN, entity.SN, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        if(original is StoredProc proc && schema.StoredProcs != null)
        {
            foreach(var candidate in schema.StoredProcs)
            {
                if(string.Equals(candidate.SN, proc.SN, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        if(original is Function func && schema.Functions != null)
        {
            foreach(var candidate in schema.Functions)
            {
                if(string.Equals(candidate.SN, func.SN, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        return null;
    }

    /// <summary>
    /// Keeps the generated content instead of writing it.
    /// </summary>
    private sealed class CapturingWriter
    {
        public List<CodeFileViewModel> Files { get; } = new List<CodeFileViewModel>();

        public void Write(List<CodeFile> filesInModel, string folder, string fileName, string content)
        {
            if(string.IsNullOrWhiteSpace(content))
                return;

            Files.Add(new CodeFileViewModel { FileName = fileName, Content = IOUtils.ToCrlf(content) });
        }
    }
}

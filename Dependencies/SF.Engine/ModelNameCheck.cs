#region Usings
using SF.Models;
using System;
using System.Collections.Generic;
#endregion

namespace SF.Engine;

/// <summary>
/// Finds the names in a saved model that the app does not support: a database, schema, object or column name with
/// characters the schema reader cannot use, or a name that cannot become a C# name. Sync and generate refuse the model instead of failing later or writing files the
/// schema reader could never read back.
/// </summary>
public static class ModelNameCheck
{
    /// <summary>
    /// One line per problem, in the model's order; empty when every name is supported.
    /// </summary>
    public static List<string> Find(ModelContainer container)
    {
        var result = new List<string>();
        if(container == null || container.Models == null)
            return result;

        foreach(var model in container.Models)
        {
            if(model == null)
                continue;

            if(model.ConInfo != null && !string.IsNullOrWhiteSpace(model.ConInfo.Database) && !NameRules.IsSupportedObjectName(model.ConInfo.Database))
                result.Add($"Model {model.Name}: the database name {model.ConInfo.Database} has characters that are not supported");

            if(model.Categories == null)
                continue;

            foreach(var category in model.Categories)
            {
                if(category.Schemas == null)
                    continue;

                var label = Label(category.Name);
                foreach(var schema in category.Schemas)
                {
                    if(!NameRules.IsSupportedSchemaName(schema.Name))
                        result.Add($"Model {model.Name}: {label}: the schema name {schema.Name} has characters that are not supported");

                    foreach(var name in ObjectNames(schema, category.Name))
                    {
                        if(!NameRules.IsSupportedObjectName(name))
                            result.Add($"Model {model.Name}: {label}: {schema.Name}.{name} (the name has characters that are not supported)");
                    }

                    AddNullNames(result, model.Name, category.Name, schema, label);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Reports every entity, property, parameter and result column whose C# name could not be made (a null N), every
    /// procedure, function and sequence whose name is not a C# identifier, and every column and result column name
    /// with characters that are not supported.
    /// </summary>
    private static void AddNullNames(List<string> result, string modelName, string categoryName, Schema schema, string label)
    {
        if(string.Equals(categoryName, CategoryNames.Entities, StringComparison.Ordinal) && schema.Entities != null)
        {
            foreach(var entity in schema.Entities)
                AddNullEntityNames(result, modelName, label, schema.Name, entity);
        }
        else if(string.Equals(categoryName, CategoryNames.Views, StringComparison.Ordinal) && schema.Views != null)
        {
            foreach(var view in schema.Views)
                AddNullEntityNames(result, modelName, label, schema.Name, view);
        }
        else if(string.Equals(categoryName, CategoryNames.Procs, StringComparison.Ordinal) && schema.StoredProcs != null)
        {
            foreach(var proc in schema.StoredProcs)
            {
                var prefix = $"Model {modelName}: {label}: {schema.Name}.{proc.SN}";
                if(!NameRules.IsValidIdentifier(proc.N))
                    result.Add($"{prefix} (the name cannot become a C# name)");

                AddNullPropertyNames(result, prefix, proc.Par, "parameter");
                AddUnsupportedColumnNames(result, prefix, proc.En == null ? null : proc.En.Ps, "result column");
                AddNullPropertyNames(result, prefix, proc.En == null ? null : proc.En.Ps, "result column");
            }
        }
        else if(string.Equals(categoryName, CategoryNames.Funcs, StringComparison.Ordinal) && schema.Functions != null)
        {
            foreach(var func in schema.Functions)
            {
                var prefix = $"Model {modelName}: {label}: {schema.Name}.{func.SN}";
                if(!NameRules.IsValidIdentifier(func.N))
                    result.Add($"{prefix} (the name cannot become a C# name)");

                AddNullPropertyNames(result, prefix, func.Par, "parameter");
                AddUnsupportedColumnNames(result, prefix, func.En == null ? null : func.En.Ps, "result column");
                AddNullPropertyNames(result, prefix, func.En == null ? null : func.En.Ps, "result column");
            }
        }
        else if(string.Equals(categoryName, CategoryNames.Seqs, StringComparison.Ordinal) && schema.Sequences != null)
        {
            foreach(var sequence in schema.Sequences)
            {
                if(!NameRules.IsValidIdentifier(sequence.N))
                    result.Add($"Model {modelName}: {label}: {schema.Name}.{sequence.SN} (the name cannot become a C# name)");
            }
        }
    }

    private static void AddNullEntityNames(List<string> result, string modelName, string label, string schemaName, Entity entity)
    {
        var prefix = $"Model {modelName}: {label}: {schemaName}.{entity.SN}";
        if(entity.N == null)
            result.Add($"{prefix} (the name cannot become a C# name)");

        AddUnsupportedColumnNames(result, prefix, entity.Ps, "column");
        AddNullPropertyNames(result, prefix, entity.Ps, "column");
    }

    private static void AddUnsupportedColumnNames(List<string> result, string prefix, PropertyCollection props, string what)
    {
        if(props == null)
            return;

        foreach(var p in props)
        {
            if(!NameRules.IsSupportedColumnName(p.SN))
                result.Add($"{prefix} ({what} '{p.SN}' has characters that are not supported)");
        }
    }

    private static void AddNullPropertyNames(List<string> result, string prefix, PropertyCollection props, string what)
    {
        if(props == null)
            return;

        foreach(var p in props)
        {
            if(p.N == null)
                result.Add($"{prefix} ({what} '{p.SN}' cannot become a C# name)");
        }
    }

    private static string Label(string categoryName)
    {
        if(string.Equals(categoryName, CategoryNames.Entities, StringComparison.Ordinal))
            return "Tables";

        if(string.Equals(categoryName, CategoryNames.Procs, StringComparison.Ordinal))
            return "Stored procedures";

        return categoryName;
    }

    private static List<string> ObjectNames(Schema schema, string categoryName)
    {
        var names = new List<string>();
        if(string.Equals(categoryName, CategoryNames.Entities, StringComparison.Ordinal) && schema.Entities != null)
        {
            foreach(var entity in schema.Entities)
                names.Add(entity.SN);
        }
        else if(string.Equals(categoryName, CategoryNames.Views, StringComparison.Ordinal) && schema.Views != null)
        {
            foreach(var view in schema.Views)
                names.Add(view.SN);
        }
        else if(string.Equals(categoryName, CategoryNames.Procs, StringComparison.Ordinal) && schema.StoredProcs != null)
        {
            foreach(var proc in schema.StoredProcs)
                names.Add(proc.SN);
        }
        else if(string.Equals(categoryName, CategoryNames.Funcs, StringComparison.Ordinal) && schema.Functions != null)
        {
            foreach(var func in schema.Functions)
                names.Add(func.SN);
        }
        else if(string.Equals(categoryName, CategoryNames.Seqs, StringComparison.Ordinal) && schema.Sequences != null)
        {
            foreach(var sequence in schema.Sequences)
                names.Add(sequence.SN);
        }

        return names;
    }
}

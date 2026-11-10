#region Usings
using SF.Models;
using System;
using System.Collections.Generic;
#endregion

namespace SF.Engine;

/// <summary>
/// sql_variant columns are skipped from every SQL composition as if they did not exist: too complex to handle and out
/// of the app's scope. The filter runs on a copy just before generation; the .edmj and the model the app shows are
/// unchanged. A key, a procedure or a function that cannot work without the column is dropped too: a procedure or a
/// function whose result has a sql_variant column is dropped as a whole (its results are read by position, and the
/// database still returns the column), and so is a table or a view that has only sql_variant columns.
/// </summary>
public static class SqlVariantFilter
{
    private const int _sqlVariantTypeId = 24;

    /// <summary>
    /// The model container to generate from: <paramref name="source"/> itself when it has no sql_variant column, so
    /// the output of such a model is unchanged by construction; otherwise a filtered copy.
    /// </summary>
    public static ModelContainer Apply(ModelContainer source, IProgressLog log)
    {
        if(!HasSqlVariant(source))
            return source;

        var copy = new ModelContainer
        {
            CommonSettings = source.CommonSettings.Clone(),
            CommonLayers = source.CommonLayers.Clone(),
        };

        foreach(var model in source.Models)
            copy.Models.Add(model.Clone());

        foreach(var model in copy.Models)
            Filter(model, log);

        return copy;
    }

    private static bool HasSqlVariant(ModelContainer container)
    {
        if(container?.Models == null)
            return false;

        foreach(var model in container.Models)
        {
            if(model.Categories == null)
                continue;

            foreach(var category in model.Categories)
            {
                if(category.Schemas == null)
                    continue;

                foreach(var schema in category.Schemas)
                {
                    if(HasSqlVariantIn(schema.Entities) || HasSqlVariantIn(schema.Views))
                        return true;

                    if(schema.StoredProcs != null)
                    {
                        foreach(var proc in schema.StoredProcs)
                        {
                            if(AnySqlVariant(proc.Par) || (proc.En != null && AnySqlVariant(proc.En.Ps)))
                                return true;
                        }
                    }

                    if(schema.Functions != null)
                    {
                        foreach(var func in schema.Functions)
                        {
                            if(AnySqlVariant(func.Par) || (func.Sc != null && IsSqlVariant(func.Sc)) || (func.En != null && AnySqlVariant(func.En.Ps)))
                                return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static bool HasSqlVariantIn(EntityCollection entities)
    {
        if(entities == null)
            return false;

        foreach(var entity in entities)
        {
            if(AnySqlVariant(entity.Ps))
                return true;
        }

        return false;
    }

    private static bool AnySqlVariant(PropertyCollection props)
    {
        if(props == null)
            return false;

        foreach(var p in props)
        {
            if(IsSqlVariant(p))
                return true;
        }

        return false;
    }

    private static bool IsSqlVariant(Property p)
    {
        return p.TId == _sqlVariantTypeId;
    }

    private static void Filter(Model model, IProgressLog log)
    {
        FilterForeignKeys(model, log);

        foreach(var category in model.Categories)
        {
            if(category.Schemas == null)
                continue;

            foreach(var schema in category.Schemas)
            {
                if(string.Equals(category.Name, CategoryNames.Entities, StringComparison.Ordinal))
                    FilterEntities(schema, schema.Entities, log);
                else if(string.Equals(category.Name, CategoryNames.Views, StringComparison.Ordinal))
                    FilterEntities(schema, schema.Views, log);
                else if(string.Equals(category.Name, CategoryNames.Procs, StringComparison.Ordinal))
                    FilterStoredProcs(schema, log);
                else if(string.Equals(category.Name, CategoryNames.Funcs, StringComparison.Ordinal))
                    FilterFunctions(schema, log);
            }
        }
    }

    private static void FilterForeignKeys(Model model, IProgressLog log)
    {
        if(model.FKeys == null)
            return;

        for(var i = model.FKeys.Count - 1; i >= 0; i--)
        {
            var fk = model.FKeys[i];
            var parent = FindEntity(model, fk.PSc, fk.PT);
            var child = FindEntity(model, fk.CSc, fk.CT);

            var drop = false;
            if(fk.FKParts != null)
            {
                foreach(var part in fk.FKParts)
                {
                    if(HasSqlVariantColumn(parent, part.PCol) || HasSqlVariantColumn(child, part.CCol))
                    {
                        drop = true;
                        break;
                    }
                }
            }

            if(!drop)
                continue;

            model.FKeys.RemoveAt(i);
            RemoveNavProps(parent, fk.Id);
            RemoveNavProps(child, fk.Id);
            log.Info($"Skipped foreign key {fk.N}: it has a sql_variant column");
        }
    }

    private static Entity FindEntity(Model model, string schemaName, string tableName)
    {
        var category = model.Categories?.GetByName(CategoryNames.Entities);
        if(category?.Schemas == null)
            return null;

        foreach(var schema in category.Schemas)
        {
            if(schema.Entities == null || !string.Equals(schema.Name, schemaName, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach(var entity in schema.Entities)
            {
                if(string.Equals(entity.SN, tableName, StringComparison.OrdinalIgnoreCase))
                    return entity;
            }
        }

        return null;
    }

    private static bool HasSqlVariantColumn(Entity entity, string storeName)
    {
        if(entity?.Ps == null)
            return false;

        foreach(var p in entity.Ps)
        {
            if(string.Equals(p.SN, storeName, StringComparison.OrdinalIgnoreCase))
                return IsSqlVariant(p);
        }

        return false;
    }

    private static void RemoveNavProps(Entity entity, int fkId)
    {
        if(entity?.NPs == null)
            return;

        for(var i = entity.NPs.Count - 1; i >= 0; i--)
        {
            if(entity.NPs[i].FKId == fkId)
                entity.NPs.RemoveAt(i);
        }

        if(entity.NPs.Count == 0)
            entity.NPs = null;
    }

    private static void FilterEntities(Schema schema, EntityCollection entities, IProgressLog log)
    {
        if(entities == null)
            return;

        for(var i = entities.Count - 1; i >= 0; i--)
        {
            var entity = entities[i];
            var removed = FilterEntity(schema.Name, entity, log);

            //nothing is left to read or write: the table or view is skipped as a whole
            if(removed > 0 && entity.Ps.Count == 0)
            {
                entities.RemoveAt(i);
                log.Info($"Skipped {schema.Name}.{entity.SN}: it has only sql_variant columns");
            }
        }
    }

    /// <summary>
    /// Removes the sql_variant columns of a table or view and the keys that use them; returns how many columns went.
    /// </summary>
    private static int FilterEntity(string schemaName, Entity entity, IProgressLog log)
    {
        if(entity.Ps == null)
            return 0;

        var sqlVariantNames = new List<string>();
        foreach(var p in entity.Ps)
        {
            if(IsSqlVariant(p))
                sqlVariantNames.Add(p.N);
        }

        if(sqlVariantNames.Count == 0)
            return 0;

        if(entity.PK != null && HasKeyPart(entity.PK, sqlVariantNames))
        {
            log.Info($"Skipped primary key {schemaName}.{entity.SN}.{entity.PK.N}: it has a sql_variant column");
            entity.PK = null;
        }

        if(entity.UKs != null)
        {
            for(var i = entity.UKs.Count - 1; i >= 0; i--)
            {
                if(HasKeyPart(entity.UKs[i], sqlVariantNames))
                {
                    log.Info($"Skipped unique key {schemaName}.{entity.SN}.{entity.UKs[i].N}: it has a sql_variant column");
                    entity.UKs.RemoveAt(i);
                }
            }

            if(entity.UKs.Count == 0)
                entity.UKs = null;
        }

        foreach(var p in entity.Ps)
        {
            if(IsSqlVariant(p))
                log.Info($"Skipped sql_variant column {schemaName}.{entity.SN}.{p.SN}");
        }

        for(var i = entity.Ps.Count - 1; i >= 0; i--)
        {
            if(IsSqlVariant(entity.Ps[i]))
                entity.Ps.RemoveAt(i);
        }

        return sqlVariantNames.Count;
    }

    private static bool HasKeyPart(Key key, List<string> sqlVariantNames)
    {
        if(key.Ps == null)
            return false;

        foreach(var part in key.Ps)
        {
            foreach(var name in sqlVariantNames)
            {
                if(string.Equals(part.N, name, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static void FilterStoredProcs(Schema schema, IProgressLog log)
    {
        if(schema.StoredProcs == null)
            return;

        for(var i = schema.StoredProcs.Count - 1; i >= 0; i--)
        {
            var proc = schema.StoredProcs[i];
            if(AnySqlVariant(proc.Par))
            {
                schema.StoredProcs.RemoveAt(i);
                log.Info($"Skipped stored procedure {schema.Name}.{proc.N}: it has a sql_variant parameter");
                continue;
            }

            if(proc.En != null && AnySqlVariant(proc.En.Ps))
            {
                schema.StoredProcs.RemoveAt(i);
                log.Info($"Skipped stored procedure {schema.Name}.{proc.N}: its result has a sql_variant column");
            }
        }
    }

    private static void FilterFunctions(Schema schema, IProgressLog log)
    {
        if(schema.Functions == null)
            return;

        for(var i = schema.Functions.Count - 1; i >= 0; i--)
        {
            var func = schema.Functions[i];
            if(AnySqlVariant(func.Par))
            {
                schema.Functions.RemoveAt(i);
                log.Info($"Skipped function {schema.Name}.{func.N}: it has a sql_variant parameter");
                continue;
            }

            if(func.Sc != null && IsSqlVariant(func.Sc))
            {
                schema.Functions.RemoveAt(i);
                log.Info($"Skipped function {schema.Name}.{func.N}: it has a sql_variant return");
                continue;
            }

            if(func.En != null && AnySqlVariant(func.En.Ps))
            {
                schema.Functions.RemoveAt(i);
                log.Info($"Skipped function {schema.Name}.{func.N}: its result has a sql_variant column");
            }
        }
    }
}

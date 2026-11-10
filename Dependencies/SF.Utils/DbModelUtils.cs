#region Usings
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SF.Db;
using SF.Models;
#endregion

namespace SF.Utils;

public static class DbModelUtils
{
    public static Dictionary<string, Dictionary<string, DbObject>> GetSelectedDbObjects(DbObjectCategory category)
    {
        if(category.Schemas == null || category.Schemas.Count < 1)
            return null;

        var selectedSchemas = new Dictionary<string, Dictionary<string, DbObject>>(category.Schemas.Count);

        foreach(var schema in category.Schemas)
        {
            var selected = new List<DbObject>(schema.Objects.Count);
            foreach(var o in schema.Objects)
            {
                if(o.IsSelected.HasValue && o.IsSelected.Value)
                    selected.Add(o);
            }

            var selectedObjects = new Dictionary<string, DbObject>(selected.Count);
            selected.Sort(DbObjectOrder.ByName);
            foreach(var o in selected)
                selectedObjects.Add(o.Name, o);

            if(selectedObjects.Count < 1)
                continue;

            selectedSchemas.Add(schema.Name, selectedObjects);
        }

        return selectedSchemas;
    }
    public static bool ValidateConnectionInfo(ConnectionInfo conInfo)
    {
        return SqlConnector.ValidateConnectionInfo(conInfo);
    }

    /// <summary>
    /// True when the server answers; otherwise <paramref name="error"/> says why.
    /// </summary>
    public static bool ValidateConnectionInfo(ConnectionInfo conInfo, out string error)
    {
        return SqlConnector.ValidateConnectionInfo(conInfo, out error);
    }
    public static List<string> GetAvailableDatabases(ConnectionInfo conInfo)
    {
        var databases = SqlConnector.GetDatabases(conInfo);

        if(StoreUtils.IsDataTableEmpty(databases))
            return new List<string>(0);

        var result = new List<string>(databases.Rows.Count);
        foreach(var r in databases.Rows.Cast<DataRow>())
            result.Add(r["NAME"].ToString());

        return result;
    }
    public static DBObjectCategoryCollection PopulateDbObjectModel(ConnectionInfo conInfo)
    {
        var dbCategories = Defaults.DbCategories();

        PopulateDbCategory(DbObjectType.Table, conInfo, dbCategories);
        PopulateDbCategory(DbObjectType.View, conInfo, dbCategories);
        PopulateDbCategory(DbObjectType.Proc, conInfo, dbCategories);
        PopulateDbCategory(DbObjectType.Func, conInfo, dbCategories);
        PopulateDbCategory(DbObjectType.Seq, conInfo, dbCategories);

        return dbCategories;
    }
    private static void PopulateDbCategory(DbObjectType type, ConnectionInfo conInfo, DBObjectCategoryCollection dbCategories)
    {
        var tableCategory = (DbObjectCategory)null;
        var invalid = (HashSet<string>)null;
        var dbObjects = (List<DbObject>)null;
        var checkPrefix = false;
        switch(type)
        {
            case DbObjectType.Table:
                tableCategory = dbCategories.GetByName(DbCategoryNames.Entities);
                invalid = UtilsLookup.Invalid.Tables;
                dbObjects = ConvertDataTableToDbObjectList(type, SqlConnector.GetTables(conInfo));
                break;
            case DbObjectType.View:
                tableCategory = dbCategories.GetByName(DbCategoryNames.Views);
                invalid = UtilsLookup.Invalid.Views;
                dbObjects = ConvertDataTableToDbObjectList(type, SqlConnector.GetViews(conInfo));
                checkPrefix = true;
                break;
            case DbObjectType.Proc:
                tableCategory = dbCategories.GetByName(DbCategoryNames.Procs);
                invalid = UtilsLookup.Invalid.Procs;
                dbObjects = ConvertDataTableToDbObjectList(type, SqlConnector.GetProcs(conInfo));
                break;
            case DbObjectType.Func:
                tableCategory = dbCategories.GetByName(DbCategoryNames.Funcs);
                invalid = UtilsLookup.Invalid.Funcs;
                dbObjects = ConvertDataTableToDbObjectList(type, SqlConnector.GetFuncs(conInfo));
                break;
            case DbObjectType.Seq:
                tableCategory = dbCategories.GetByName(DbCategoryNames.Seqs);
                invalid = UtilsLookup.Invalid.Seqs;
                dbObjects = ConvertDataTableToDbObjectList(type, SqlConnector.GetSequences(conInfo));
                break;
            default:
                throw new ArgumentException($"DbObjectType {type} is not supported.");
        }

        if(dbObjects == null || dbObjects.Count < 1)
            return;

        var label = (string)null;
        switch(type)
        {
            case DbObjectType.Table:
                label = "Tables";
                break;
            case DbObjectType.View:
                label = "Views";
                break;
            case DbObjectType.Proc:
                label = "Stored procedures";
                break;
            case DbObjectType.Func:
                label = "Functions";
                break;
            case DbObjectType.Seq:
                label = "Sequences";
                break;
        }

        dbObjects.Sort(DbObjectOrder.BySchemaThenName);

        foreach(var t in dbObjects)
        {
            var nameLower = t.Name.ToLower();

            if(invalid.Contains(nameLower))
                continue;

            if(!NameRules.IsSupportedSchemaName(t.SchemaName))
            {
                dbCategories.NotSupported.Add($"{label}: {t.SchemaName}.{t.Name} (the schema name has characters that are not supported)");
                continue;
            }

            if(!NameRules.IsSupportedObjectName(t.Name))
            {
                dbCategories.NotSupported.Add($"{label}: {t.SchemaName}.{t.Name} (the name has characters that are not supported)");
                continue;
            }

            if(checkPrefix && nameLower.StartsWith("syncobj_", StringComparison.Ordinal))
                continue;

            var schema = (DbObjectSchema)null;
            if(!tableCategory.Schemas.Contains(t.SchemaName))
            {
                schema = new DbObjectSchema { Name = t.SchemaName, CategoryType = type, Objects = new DbObjectCollection() };
                tableCategory.Schemas.Add(schema);
            }
            else
            {
                schema = tableCategory.Schemas.GetByName(t.SchemaName);
            }

            schema.Objects.Add(t);
        }
    }
    private static List<DbObject> ConvertDataTableToDbObjectList(DbObjectType type, DataTable dataTable)
    {
        var nameParam = (string)null;
        var schemaParam = (string)null;
        switch(type)
        {
            case DbObjectType.Table:
            case DbObjectType.View:
                nameParam = SqlTable.Params.Name;
                schemaParam = SqlTable.Params.Schema;
                return ConvertRowsToDbObjects(type, dataTable, nameParam, schemaParam);

            case DbObjectType.Proc:
                nameParam = SqlRoutine.Params.Name;
                schemaParam = SqlRoutine.Params.Schema;
                return ConvertRowsToDbObjects(type, dataTable, nameParam, schemaParam);
            case DbObjectType.Func:
                nameParam = SqlRoutine.Params.Name;
                schemaParam = SqlRoutine.Params.Schema;
                var functions = new List<DbObject>(dataTable.Rows.Count);
                foreach(var t in dataTable.Rows.Cast<DataRow>())
                {
                    var isTable = string.CompareOrdinal(t[SqlParams.Params.DataType].ToString().ToUpper(), "TABLE") == 0;
                    functions.Add(new DbObject
                    {
                        Name = t[nameParam].ToString(),
                        SchemaName = t[schemaParam].ToString(),
                        Type = type,
                        FunctionKind = isTable ? FunctionKind.Table : FunctionKind.Scalar,
                        Return = !isTable ? new Property//if it is not a table valued function return a scalar property else set this property to null
                        {
                            SN = null,
                            TId = Lookup.GetTypeId(t[SqlParams.Params.DataType].ToString()),
                            M = ParamMode.Out,
                            //Ord = 0,//the return type uses ordinal zero
                            S = (t[SqlParams.Params.MaxStringLength] == null || string.IsNullOrWhiteSpace(t[SqlParams.Params.MaxStringLength].ToString())) ? null : (int?)t[SqlParams.Params.MaxStringLength],
                        } : null,
                    });
                }

                return functions;
            case DbObjectType.Seq:
                nameParam = SqlSequence.Params.Name;
                schemaParam = SqlSequence.Params.Schema;
                var sequences = new List<DbObject>(dataTable.Rows.Count);
                foreach(var t in dataTable.Rows.Cast<DataRow>())
                {
                    sequences.Add(new DbObject
                    {
                        Name = t[nameParam].ToString(),
                        SchemaName = t[schemaParam].ToString(),
                        Type = type,
                        FunctionKind = FunctionKind.Scalar,
                        Return = new Property//if it is not a table valued function return a scalar property else set this property to null
                        {
                            SN = null,
                            TId = Lookup.GetTypeId(t[SqlSequence.Params.DataType].ToString()),
                            M = ParamMode.Out,
                        },
                    });
                }

                return sequences;
            default:
                throw new ArgumentException($"DbObjectType {type} is not supported.");
        }
    }
    private static List<DbObject> ConvertRowsToDbObjects(DbObjectType type, DataTable dataTable, string nameParam, string schemaParam)
    {
        var result = new List<DbObject>(dataTable.Rows.Count);
        foreach(var t in dataTable.Rows.Cast<DataRow>())
        {
            result.Add(new DbObject
            {
                Name = t[nameParam].ToString(),
                SchemaName = t[schemaParam].ToString(),
                Type = type
            });
        }

        return result;
    }
    public static void OverlayCurrentModel(Model model, DBObjectCategoryCollection dbCategories)
    {
        OverlayEntities(model, dbCategories);
        OverlayViews(model, dbCategories);
        OverlaySProcs(model, dbCategories);
        OverlayFuncs(model, dbCategories);
        OverlaySeqs(model, dbCategories);
    }
    private static void OverlayEntities(Model model, DBObjectCategoryCollection dbCategories)
    {
        var cat = model.Categories.GetByName(CategoryNames.Entities);
        var dbCat = dbCategories.GetByName(DbCategoryNames.Entities);//get the category from the DbObjectCategoryCollection
        foreach(var schema in cat.Schemas)
        {
            if(schema.Count < 1)
                continue;

            if(!dbCat.Schemas.Contains(schema.Name))
                continue;

            var dbSchema = dbCat.Schemas.GetByName(schema.Name);

            foreach(var o in schema.Entities)
            {
                if(dbSchema.Objects.Contains(o.SN))
                    dbSchema.Objects.GetByName(o.SN).IsSelected = true;
            }
        }
    }
    private static void OverlayViews(Model model, DBObjectCategoryCollection dbCategories)
    {
        var cat = model.Categories.GetByName(CategoryNames.Views);
        var dbCat = dbCategories.GetByName(DbCategoryNames.Views);//get the category from the DbObjectCategoryCollection
        foreach(var schema in cat.Schemas)
        {
            if(schema.Count < 1)
                continue;

            if(!dbCat.Schemas.Contains(schema.Name))
                continue;

            var dbSchema = dbCat.Schemas.GetByName(schema.Name);

            foreach(var o in schema.Views)
            {
                if(dbSchema.Objects.Contains(o.SN))
                    dbSchema.Objects.GetByName(o.SN).IsSelected = true;
            }
        }
    }
    private static void OverlaySProcs(Model model, DBObjectCategoryCollection dbCategories)
    {
        var cat = model.Categories.GetByName(CategoryNames.Procs);
        var dbCat = dbCategories.GetByName(DbCategoryNames.Procs);//get the category from the DbObjectCategoryCollection
        foreach(var schema in cat.Schemas)
        {
            if(schema.Count < 1)
                continue;

            if(!dbCat.Schemas.Contains(schema.Name))
                continue;

            var dbSchema = dbCat.Schemas.GetByName(schema.Name);

            foreach(var o in schema.StoredProcs)
            {
                if(dbSchema.Objects.Contains(o.SN))
                    dbSchema.Objects.GetByName(o.SN).IsSelected = true;
            }
        }
    }
    private static void OverlayFuncs(Model model, DBObjectCategoryCollection dbCategories)
    {
        var cat = model.Categories.GetByName(CategoryNames.Funcs);
        var dbCat = dbCategories.GetByName(DbCategoryNames.Funcs);//get the category from the DbObjectCategoryCollection
        foreach(var schema in cat.Schemas)
        {
            if(schema.Count < 1)
                continue;

            if(!dbCat.Schemas.Contains(schema.Name))
                continue;

            var dbSchema = dbCat.Schemas.GetByName(schema.Name);

            foreach(var o in schema.Functions)
            {
                if(dbSchema.Objects.Contains(o.SN))
                    dbSchema.Objects.GetByName(o.SN).IsSelected = true;
            }
        }
    }
    private static void OverlaySeqs(Model model, DBObjectCategoryCollection dbCategories)
    {
        //this change is for backwards compatibility.  since this category is new.  then existing models will not contain this category.
        //this code will be removed in the future.
        if(!model.Categories.Contains(CategoryNames.Seqs))
            model.Categories.Add(new Category { Name = CategoryNames.Seqs });
        //end of backwards compatibility fix

        var cat = model.Categories.GetByName(CategoryNames.Seqs);
        var dbCat = dbCategories.GetByName(DbCategoryNames.Seqs);//get the category from the DbObjectCategoryCollection
        foreach(var schema in cat.Schemas)
        {
            if(schema.Count < 1)
                continue;

            if(!dbCat.Schemas.Contains(schema.Name))
                continue;

            var dbSchema = dbCat.Schemas.GetByName(schema.Name);

            foreach(var o in schema.Sequences)
            {
                if(dbSchema.Objects.Contains(o.SN))
                    dbSchema.Objects.GetByName(o.SN).IsSelected = true;
            }
        }
    }
}

#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using SF.Db;
using SF.Models;
#endregion

namespace SF.Utils;

internal sealed class StoreUtils
{
    public static Dictionary<string, List<Property>> GetProperties(ConnectionInfo conInfo, string schema, string[] tableNames)
    {//return tablename as key
        var raw = SqlConnector.GetColumns(conInfo, schema, tableNames);

        if(IsDataTableEmpty(raw))
            return new Dictionary<string, List<Property>>(0);

        var result = new Dictionary<string, List<Property>>(tableNames.Length);
        foreach(var c in raw.Rows.Cast<System.Data.DataRow>())
        {
            var tableName = c[SqlColumn.Params.TableName].ToString();
            var props = (List<Property>)null;
            if(result.ContainsKey(tableName))
            {
                props = result[tableName];
            }
            else
            {
                props = new List<Property>();
                result.Add(tableName, props);
            }

            var sizeObj = c[SqlColumn.Params.MaxStringLength];
            var storeName = c[SqlColumn.Params.Name].ToString();
            var sqlDatType = c[SqlColumn.Params.DataType].ToString().ToLower();
            var defValObj = c[SqlColumn.Params.DefaultValue];
            var defaultVal = defValObj != null ? defValObj.ToString() : null;
            var p = new Property
            {
                SN = storeName,
                TId = Lookup.GetTypeId(sqlDatType),
                Def = string.IsNullOrWhiteSpace(defaultVal) ? null : defaultVal,
                Nul = string.CompareOrdinal(c[SqlColumn.Params.IsNullable]?.ToString().ToUpper(), "YES") == 0 ? YesOrNo.Yes : YesOrNo.No,
                IsComp = Lookup.AutoComputedTypes.Contains(sqlDatType) ? YesOrNo.Yes : YesOrNo.No,
                //Ord = (int)c[SqlColumn.Params.Ordinal],
                S = (sizeObj == null || string.IsNullOrWhiteSpace(sizeObj.ToString())) ? null : (int?)sizeObj,
            };
            if(string.CompareOrdinal(p.T, "byte[]") == 0 && !string.IsNullOrWhiteSpace(p.Def) && p.Def.Length > 3995)
            {//if it is a byte array check if it has a default value in the sys.columns table. because the information_schema truncates this value if it is longer than 4000
                var defRaw = SqlConnector.GetColumnDefaultValue(conInfo, schema, tableName, p.SN);
                if(!IsDataTableEmpty(defRaw))
                {
                    var defVal = defRaw.Rows[0][SqlColumn.Params.DefaultValue];
                    p.Def = defVal?.ToString();
                }
            }

            props.Add(p);
        }

        return result;
    }
    /// <summary>
    /// Every key of the database, read once for all schemas; <see cref="GetKeys"/> takes one schema's from it.
    /// </summary>
    public static System.Data.DataTable GetAllKeys(ConnectionInfo conInfo)
    {
        return SqlConnector.GetAllKeys(conInfo);
    }

    /// <summary>
    /// Every foreign key of the database, read once for all schemas; <see cref="GetFKeys"/> takes one schema's from it.
    /// </summary>
    public static System.Data.DataTable GetAllFKeys(ConnectionInfo conInfo)
    {
        return SqlConnector.GetAllForeignKeys(conInfo);
    }

    /// <summary>
    /// The rows of <paramref name="all"/> (schema in its first column) of one schema and the given tables, in order.
    /// </summary>
    private static System.Data.DataTable RowsOf(System.Data.DataTable all, string schemaColumn, string tableColumn, string schema, string[] tableNames)
    {
        var result = all.Clone();
        var tables = new HashSet<string>(tableNames ?? new string[0], StringComparer.Ordinal);
        foreach(var row in all.Rows.Cast<System.Data.DataRow>())
        {
            if(string.Equals(row[schemaColumn].ToString(), schema, StringComparison.Ordinal) && tables.Contains(row[tableColumn].ToString()))
                result.ImportRow(row);
        }

        return result;
    }

    public static Dictionary<string, Dictionary<string, List<DbKey>>> GetKeys(System.Data.DataTable allKeys, string schema, string[] tableNames)
    {//returns table as key, key type as second key
        var raw = RowsOf(allKeys, "TABLE_SCHEMA", SqlKey.Params.TableName, schema, tableNames);

        if(IsDataTableEmpty(raw))
            return new Dictionary<string, Dictionary<string, List<DbKey>>>(0);

        var colsDic = new Dictionary<string, Dictionary<string, List<System.Data.DataRow>>>();
        foreach(var r in raw.Rows.Cast<System.Data.DataRow>())
        {
            var tableName = r[SqlKey.Params.TableName].ToString();
            if(!colsDic.TryGetValue(tableName, out var keysByTable))
            {
                keysByTable = new Dictionary<string, List<System.Data.DataRow>>();
                colsDic.Add(tableName, keysByTable);
            }

            var keyName = r[SqlKey.Params.Name].ToString();
            if(!keysByTable.TryGetValue(keyName, out var keyRows))
            {
                keyRows = new List<System.Data.DataRow>();
                keysByTable.Add(keyName, keyRows);
            }

            keyRows.Add(r);
        }

        foreach(var keysByTable in colsDic.Values)
        {
            foreach(var keyName in keysByTable.Keys.ToArray())
                keysByTable[keyName] = keysByTable[keyName].OrderBy(GetKeyOrdinal).ToList();//stable sort: keeps the database order for equal ordinals
        }

        var tables = colsDic.Keys.ToArray();
        var tableCount = tables.Length;
        var result = new Dictionary<string, Dictionary<string, List<DbKey>>>(tableCount);
        for(var i = 0; i < tableCount; i++)
        {
            var tableName = tables[i];
            var keysByTable = colsDic[tableName];
            var keyNames = keysByTable.Keys.ToArray();
            var keyCount = keyNames.Length;
            var pkeys = new List<DbKey>(keyCount);
            var ukeys = new List<DbKey>(keyCount);
            var keys = new Dictionary<string, List<DbKey>>(2)
            {
                { KeyTypes.PK, pkeys },
                { KeyTypes.UK, ukeys }
            };

            for(var b = 0; b < keyCount; b++)
            {
                var keyName = keyNames[b];
                var keysByName = keysByTable[keyName];
                var keyType = keysByName[0][SqlKey.Params.Type].ToString().ToUpper();
                var newKey = new DbKey
                {
                    SN = keyName,
                    Ps = new List<DbKeyProperty>(),
                };
                foreach(var k in keysByName)
                {
                    newKey.Ps.Add(new DbKeyProperty
                    {
                        SN = k[SqlKey.Params.ColumnName].ToString()
                    });
                }

                if(string.CompareOrdinal(KeyTypes.UK, keyType) == 0)
                    ukeys.Add(newKey);
                else
                    pkeys.Add(newKey);
            }

            result.Add(tableName, keys);
        }

        return result;
    }
    public static Dictionary<string, FKey> GetFKeys(System.Data.DataTable allFKeys, string schema, string[] tableNames)
    {//this returns FKName as the key of the dictionary. FKey is NOT by table( entity )..... Tables(entities) have NavProperties
        var raw = RowsOf(allFKeys, "FK_CONSTRAINT_SCHEMA", SqlFKey.Params.ChildTable, schema, tableNames);

        if(IsDataTableEmpty(raw))
            return new Dictionary<string, FKey>(0);

        var fkeysDic = new Dictionary<string, List<System.Data.DataRow>>();
        foreach(var r in raw.Rows.Cast<System.Data.DataRow>())
        {
            var name = r[SqlFKey.Params.FKName].ToString();
            if(!fkeysDic.TryGetValue(name, out var rows))
            {
                rows = new List<System.Data.DataRow>();
                fkeysDic.Add(name, rows);
            }

            rows.Add(r);
        }

        var fkNames = fkeysDic.Keys.ToArray();
        var fkCount = fkNames.Length;
        var result = new Dictionary<string, FKey>(fkCount);

        for(var i = 0; i < fkCount; i++)
        {
            var fkName = fkNames[i];
            var fkRows = fkeysDic[fkName];
            foreach(var fkRow in fkRows)
            {
                fkName = fkName.Replace('-', '_').Replace(' ', '_');
                var fKey = (FKey)null;
                if(result.ContainsKey(fkName))
                {
                    fKey = result[fkName];
                }
                else
                {
                    fKey = new FKey
                    {
                        Id = IdGenerator.NextNumericID(),
                        N = fkRow[SqlFKey.Params.FKName].ToString().Replace('-', '_').Replace(' ', '_'),
                        CT = fkRow[SqlFKey.Params.ChildTable].ToString(),
                        PT = fkRow[SqlFKey.Params.ParentTable].ToString(),
                        PKey = fkRow[SqlFKey.Params.ParentKey].ToString(),
                        PSc = fkRow[SqlFKey.Params.ParentSchema].ToString(),
                        CSc = schema,
                        FKParts = new FKeyPartCollection(),
                    };
                    result.Add(fkName, fKey);
                }

                var fkPart = new FKeyPart
                {
                    CCol = fkRow[SqlFKey.Params.ChildCol].ToString(),
                    PCol = fkRow[SqlFKey.Params.ParentCol].ToString(),
                    //Ord = (int)fkRow[SqlFKey.Params.FKPartOrdinal]
                };
                fKey.FKParts.Add(fkPart);
            }
        }

        return result;
    }
    public static Dictionary<string, List<IdentityComputedGeneratedInfo>> GetIdentityComputedGeneratedColumns(ConnectionInfo conInfo, string schema, string[] tableNames)
    {//the key of the result is the table name
        var raw = SqlConnector.GetIdentityComputedGeneratedColumns(conInfo, schema, tableNames);

        if(IsDataTableEmpty(raw))
            return new Dictionary<string, List<IdentityComputedGeneratedInfo>>(0);

        var result = new Dictionary<string, List<IdentityComputedGeneratedInfo>>();
        foreach(var r in raw.Rows.Cast<System.Data.DataRow>())
        {
            var info = new IdentityComputedGeneratedInfo
            {
                Entity = r[SqlIdentityComputedGenerated.Params.Table].ToString(),
                Property = r[SqlIdentityComputedGenerated.Params.Column].ToString(),
                IsIdentity = (int)r[SqlIdentityComputedGenerated.Params.IsIdentity] == 1,
                IsComputed = (int)r[SqlIdentityComputedGenerated.Params.IsComputed] == 1,
                IsGenerated = r[SqlIdentityComputedGenerated.Params.IsGenerated] != DBNull.Value && (int)r[SqlIdentityComputedGenerated.Params.IsGenerated] > 0,
            };

            if(!result.TryGetValue(info.Entity, out var infos))
            {
                infos = new List<IdentityComputedGeneratedInfo>();
                result.Add(info.Entity, infos);
            }

            infos.Add(info);
        }

        return result;
    }
    public static Dictionary<string, List<Property>> GetParamProperties(ConnectionInfo conInfo, string schema, string[] routineNames)
    {//return tablename as key
        var raw = SqlConnector.GetRoutineParams(conInfo, schema, routineNames);

        if(IsDataTableEmpty(raw))
            return new Dictionary<string, List<Property>>(0);

        var result = new Dictionary<string, List<Property>>(routineNames.Length);
        foreach(var c in raw.Rows.Cast<System.Data.DataRow>())
        {
            var dbObjectName = c[SqlParams.Params.ProcName].ToString();
            var props = (List<Property>)null;
            if(result.ContainsKey(dbObjectName))
            {
                props = result[dbObjectName];
            }
            else
            {
                props = new List<Property>();
                result.Add(dbObjectName, props);
            }

            var mode = ParamMode.In;
            var modeStr = c[SqlParams.Params.Mode].ToString();
            if(string.CompareOrdinal(modeStr, "IN") != 0)
            {
                if(string.CompareOrdinal(modeStr, "OUT") == 0)
                    mode = ParamMode.Out;
                else
                    mode = ParamMode.InOut;
            }

            var sizeObj = c[SqlParams.Params.MaxStringLength];
            var storeName = c[SqlParams.Params.Name].ToString();
            var sqlDatType = c[SqlParams.Params.DataType].ToString().ToLower();
            //these are always null for parameters.  
            //var defValObj = c[SqlColumn.Params.DefaultValue];
            //var defaultVal = defValObj != null ? defValObj.ToString() : null;

            var param = new Property
            {
                SN = storeName,
                TId = Lookup.GetTypeId(sqlDatType),
                //param.Def = string.IsNullOrWhiteSpace(defaultVal) ? null : defaultVal;
                M = mode,
                Nul = YesOrNo.Yes,//all parameters for stored procs and functions are nullable in sql.
                                  //param.Ord = (int)c[SqlParams.Params.Ordinal],
                S = (sizeObj == null || string.IsNullOrWhiteSpace(sizeObj.ToString())) ? null : (int?)sizeObj
            };
            props.Add(param);
        }

        return result;
    }
    public static Dictionary<string, Dictionary<string, List<string>>> GetTriggers(ConnectionInfo conInfo)
    {//first key is schema, second key table name, value is a list of trigger names.
        var raw = SqlConnector.GetTriggers(conInfo);

        if(IsDataTableEmpty(raw))
            return new Dictionary<string, Dictionary<string, List<string>>>(0);

        var result = new Dictionary<string, Dictionary<string, List<string>>>();
        foreach(var r in raw.Rows.Cast<System.Data.DataRow>())
        {
            var schemaName = r[SqlTrigger.Params.TableSchema].ToString();
            if(!result.TryGetValue(schemaName, out var tables))
            {
                tables = new Dictionary<string, List<string>>();
                result.Add(schemaName, tables);
            }

            var tableName = r[SqlTrigger.Params.TableName].ToString();
            if(!tables.TryGetValue(tableName, out var triggers))
            {
                triggers = new List<string>();
                tables.Add(tableName, triggers);
            }

            triggers.Add(r[SqlTrigger.Params.TriggerName].ToString());
        }

        return result;
    }
    public static Entity GetRoutineTableResultType(ConnectionInfo conInfo, string schema, string routineName, int paramCount, bool isFunction)
    {
        var raw = SqlConnector.GetRoutineReturnTypeColumns(conInfo, schema, routineName, paramCount, isFunction);

        if(IsDataTableEmpty(raw))
            return null;

        var result = new Entity
        {
            SN = routineName + "_Result",
        };

        var props = new PropertyCollection(result);
        var repeatedProps = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach(var c in raw.Rows.Cast<System.Data.DataRow>())
        {
            var isHiddenObj = c[SqlSchemaColumn.Params.IsHidden];

            if(isHiddenObj == null)
                continue;

            if(string.IsNullOrWhiteSpace(isHiddenObj.ToString()))
                continue;

            if((bool)isHiddenObj)//if it is a hidden property then just skip
                continue;

            var isNullableObj = c[SqlSchemaColumn.Params.IsNullable];

            var isNullable = isNullableObj == null || (bool)isNullableObj;

            var sizeObj = c[SqlSchemaColumn.Params.Size];
            var typeName = c[SqlSchemaColumn.Params.DataType].ToString();

            var p = new Property
            {
                SN = c[SqlSchemaColumn.Params.Name].ToString(),
                TId = Lookup.GetTypeId(c[SqlSchemaColumn.Params.DataType].ToString()),
                Def = null,
                Nul = isNullable ? YesOrNo.Yes : YesOrNo.No,
                //Ord = (int)c[SqlSchemaColumn.Params.Ordinal],
                S = (sizeObj == null || string.IsNullOrWhiteSpace(sizeObj.ToString())) ? null : (int?)sizeObj,
            };

            if(p.TId == -1)//if the type was not found check if it is geography or geomretry. it might come with a fully qualified name.
            {
                if(typeName.EndsWith(".geography", StringComparison.Ordinal))
                    p.TId = Lookup.GetTypeId("geography");
                else if(typeName.EndsWith(".geometry", StringComparison.Ordinal))
                    p.TId = Lookup.GetTypeId("geometry");
            }

            //a column whose name cannot become a C# name keeps its null N: the model builder leaves the routine out and
            //reports it
            if(p.N != null && props.Contains(p.N))
            {
                //Id, Id1, Id: the next number can be taken by a real column, so count on until the name is free
                var name = p.N;
                var storeName = p.SN;
                do
                {
                    if(!repeatedProps.ContainsKey(name))
                        repeatedProps.Add(name, 1);
                    else
                        repeatedProps[name]++;

                    p.SN = storeName + repeatedProps[name].ToString();
                }
                while(props.Contains(p.N));
            }

            props.Add(p);
        }

        result.Ps = props;
        return result;
    }

    private static int GetKeyOrdinal(System.Data.DataRow row)
    {
        return (int)row[SqlKey.Params.Ordinal];
    }

    public static bool IsDataTableEmpty(System.Data.DataTable table)
    {
        return (table == null || table.Rows == null || table.Rows.Count < 1);
    }
}

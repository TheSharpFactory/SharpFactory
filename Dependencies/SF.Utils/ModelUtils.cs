#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using SF.Models;
#endregion

namespace SF.Utils;

public static class ModelUtils
{
    public static void Save(ModelContainer modelContainer, string edmjFile)
    {
        IOUtils.SaveFile(edmjFile, JsonUtils.Serialize(modelContainer));
    }

    /// <summary>
    /// A model as the .edmj stores it, to tell whether two models are the same.
    /// </summary>
    public static string ToJson(Model model)
    {
        return JsonUtils.Serialize(model);
    }
    public static ModelContainer Load(string edmjFile)
    {
        if(string.IsNullOrWhiteSpace(edmjFile) || !IOUtils.FileExists(edmjFile))
            return null;

        var content = IOUtils.FileReadContent(edmjFile);
        if(string.IsNullOrWhiteSpace(content))
            return null;

        //var errors = new List<string>();

        var modelContainer = JsonUtils.Deserialize<ModelContainer>(content);

        //if (errors?.Count > 0)
        //{
        //  var b = new StringBuilder();
        //  b.AppendLine("Failed to deserialize model due to:");
        //  foreach (var e in errors)
        //  {
        //    b.AppendLine(e);
        //  }
        //  throw new Exception(b.ToString());
        //}

        return modelContainer;
    }

    public static void PopulateEntitiesPropsAndKeys(Model model, Dictionary<string, Dictionary<string, DbObject>> schemas, ModelObjectType objectType, List<string> notSupported = null)
    {
        var catName = "";
        var label = "";
        switch(objectType)
        {
            case ModelObjectType.Entities:
                catName = CategoryNames.Entities;
                label = "Tables";
                break;
            case ModelObjectType.Views:
                catName = CategoryNames.Views;
                label = "Views";
                break;
            default:
                throw new ArgumentException($"PopulateEntitiesPropsAndKeys does not support Object Type {objectType}");
        }

        var cInfo = model.ConInfo;
        var schemaNames = schemas.Keys.ToArray();
        var schemaCount = schemaNames.Length;
        var cat = model.Categories.GetByName(catName);
        var triggersBySchema = StoreUtils.GetTriggers(cInfo);
        var allKeys = StoreUtils.GetAllKeys(cInfo);

        for(var s = 0; s < schemaCount; s++)
        {
            var schema = schemaNames[s];
            var dbObjects = schemas[schema];
            var schemaTriggers = triggersBySchema.ContainsKey(schema) ? triggersBySchema[schema] : new Dictionary<string, List<string>>(0);

            var dbObjectNames = dbObjects.Keys.ToArray();
            var count = dbObjectNames.Length;

            var propsDic = StoreUtils.GetProperties(cInfo, schema, dbObjectNames);
            var keysDic = StoreUtils.GetKeys(allKeys, schema, dbObjectNames);

            var newSchema = new Schema
            {
                Name = schema,
                Type = objectType,
            };

            switch(objectType)
            {
                case ModelObjectType.Entities:
                    newSchema.Entities = new EntityCollection();
                    break;
                case ModelObjectType.Views:
                    newSchema.Views = new EntityCollection();
                    break;
                default:
                    throw new ArgumentException($"PopulateEntitiesPropsAndKeys does not support Object Type {objectType}");
            }

            for(var i = 0; i < count; i++)
            {
                var tableObject = dbObjects[dbObjectNames[i]];

                var entity = new Entity
                {
                    SN = tableObject.Name,
                    HTr = schemaTriggers.ContainsKey(tableObject.Name) && schemaTriggers[tableObject.Name].Count > 0 ? YesOrNo.Yes : YesOrNo.No
                };

                if(entity.N == null)
                {
                    AddNotSupported(notSupported, label, schema, tableObject.Name, "the name cannot become a C# name");
                    continue;
                }

                if(propsDic.ContainsKey(entity.SN))
                {
                    var unsupported = FirstUnsupportedColumnName(propsDic[entity.SN]);
                    if(unsupported != null)
                    {
                        AddNotSupported(notSupported, label, schema, tableObject.Name, $"column '{unsupported.SN}' has characters that are not supported");
                        continue;
                    }

                    var invalid = FirstInvalidName(propsDic[entity.SN]);
                    if(invalid != null)
                    {
                        AddNotSupported(notSupported, label, schema, tableObject.Name, $"column '{invalid.SN}' cannot become a C# name");
                        continue;
                    }

                    entity.Ps = new PropertyCollection(entity);
                    var props = propsDic[entity.SN];
                    foreach(var p in props)
                    {
                        entity.Ps.Add(p);
                    }
                }

                if(keysDic.ContainsKey(entity.SN))
                {
                    var keys = keysDic[entity.SN];
                    var pkeys = keys[KeyTypes.PK];
                    var ukeys = keys[KeyTypes.UK];

                    PopulateEntityPK(entity, pkeys);
                    PopulateEntityUKs(entity, ukeys);
                }

                switch(objectType)
                {
                    case ModelObjectType.Entities:
                        newSchema.Entities.Add(entity);
                        break;
                    case ModelObjectType.Views:
                        newSchema.Views.Add(entity);
                        break;
                    default:
                        throw new ArgumentException($"PopulateEntitiesPropsAndKeys does not support Object Type {objectType}");
                }
            }

            cat.Schemas.Add(newSchema);
        }
    }

    private static Property FirstUnsupportedColumnName(List<Property> props)
    {
        if(props == null)
            return null;

        foreach(var p in props)
        {
            if(!NameRules.IsSupportedColumnName(p.SN))
                return p;
        }

        return null;
    }

    private static Property FirstUnsupportedColumnName(PropertyCollection props)
    {
        if(props == null)
            return null;

        foreach(var p in props)
        {
            if(!NameRules.IsSupportedColumnName(p.SN))
                return p;
        }

        return null;
    }

    private static Property FirstInvalidName(List<Property> props)
    {
        if(props == null)
            return null;

        foreach(var p in props)
        {
            if(p.N == null)
                return p;
        }

        return null;
    }

    private static Property FirstInvalidName(PropertyCollection props)
    {
        if(props == null)
            return null;

        foreach(var p in props)
        {
            if(p.N == null)
                return p;
        }

        return null;
    }

    private static void AddNotSupported(List<string> notSupported, string label, string schema, string name, string reason)
    {
        if(notSupported == null)
            return;

        notSupported.Add($"{label}: {schema}.{name} ({reason})");
    }

    private static void PopulateEntityUKs(Entity entity, List<DbKey> ukeys)
    {
        if(!(ukeys?.Count > 0))
            return;

        if(entity.UKs == null)
            entity.UKs = new KeyCollection();

        foreach(var k in ukeys)
        {
            var uk = new Key
            {
                N = k.SN.Replace('-', '_').Replace(' ', '_'),
                Ps = new KeyPropertyCollection()
            };
            foreach(var kP in k.Ps)
            {
                uk.Ps.Add(new KeyProperty
                {
                    N = entity.Ps.GetByStoreName(kP.SN).N
                });
            }

            entity.UKs.Add(uk);
        }
    }

    private static void PopulateEntityPK(Entity entity, List<DbKey> pkeys)
    {
        if(!(pkeys?.Count > 0))
            return;

        var k = pkeys[0];
        var pk = new Key
        {
            N = k.SN.Replace('-', '_').Replace(' ', '_'),
            Ps = new KeyPropertyCollection()
        };

        foreach(var kP in k.Ps)
        {
            pk.Ps.Add(new KeyProperty
            {
                N = entity.Ps.GetByStoreName(kP.SN).N
            });
        }

        entity.PK = pk;
    }

    public static void PopulateForeignKeysAndNavProps(Model model, Dictionary<string, Dictionary<string, DbObject>> schemas)
    {
        var cInfo = model.ConInfo;
        var schemaNames = schemas.Keys.ToArray();
        var schemaCount = schemaNames.Length;
        var cat = model.Categories.GetByName(CategoryNames.Entities);
        var repeatedProps = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>(schemaCount);
        var allFKeys = StoreUtils.GetAllFKeys(cInfo);
        IdGenerator.Reset();
        for(var s = 0; s < schemaCount; s++)
        {
            var schemaName = schemaNames[s];
            var tables = schemas[schemaName];

            var tableNames = tables.Keys.ToArray();

            //this has the foreignkeyname as the key
            var fKeysDic = StoreUtils.GetFKeys(allFKeys, schemaName, tableNames);

            if(fKeysDic.Count < 1)
                continue;

            var fkNames = fKeysDic.Keys.ToArray();
            var fkCount = fkNames.Length;

            //check that the schema is present. otherwise skip
            if(!cat.Schemas.Contains(schemaName))
                continue;

            var childSchema = cat.Schemas.GetByName(schemaName);
            for(var i = 0; i < fkCount; i++)
            {
                var fkName = fkNames[i];
                var fk = fKeysDic[fkName];

                if(model.FKeys.Contains(fk.Id))
                    continue;

                //make sure that the child table is present (a table left out as not supported has no name or is not there)
                if(fk.CEn == null || fk.PEn == null || !childSchema.Entities.Contains(fk.CEn))
                    continue;

                //make sure the parent schema is present
                if(!cat.Schemas.Contains(fk.PSc))
                    continue;

                //get parent schema
                var parentSchema = cat.Schemas.GetByName(fk.PSc);

                //make sure that the parent table is present
                if(!parentSchema.Entities.Contains(fk.PEn))
                    continue;

                //get tables for both ends
                var parent = parentSchema.Entities.GetByName(fk.PEn);
                var child = childSchema.Entities.GetByName(fk.CEn);

                DetermineChildMultiplicity(fk, child);

                //initialize the repeated nav props dictionaries
                InitializeRepeatedNavPropForEntity(repeatedProps, parentSchema.Name, parent.N);
                InitializeRepeatedNavPropForEntity(repeatedProps, childSchema.Name, child.N);

                if(!IsFKSelfReferencing(childSchema, parentSchema, child, parent))
                {
                    //create navproperties on both entities
                    CreateNavPropertyOnEntity(repeatedProps[childSchema.Name][child.N], child, fk.Id, fk.PMu, fk.PEn, NavPropertyEnd.Parent);
                    CreateNavPropertyOnEntity(repeatedProps[parentSchema.Name][parent.N], parent, fk.Id, fk.CMu, fk.CEn, NavPropertyEnd.Child);
                }
                else
                {
                    //self referencing nav property. create only one.
                    CreateNavPropertyOnEntity(repeatedProps[childSchema.Name][child.N], child, fk.Id, fk.PMu, fk.PEn, NavPropertyEnd.Parent);
                    //CreateNavPropertyOnEntity(repeatedProps[parentSchema.Name][parent.N], parent, fk.Id, fk.CMu, fk.CEn, NavPropertyEnd.Child);
                }

                model.FKeys.Add(fk);
            }
        }
    }
    public static void ApplyIdentityComputedValues(Model model, Dictionary<string, Dictionary<string, DbObject>> schemas)
    {
        var cInfo = model.ConInfo;
        var schemaNames = schemas.Keys.ToArray();
        var schemaCount = schemaNames.Length;
        var cat = model.Categories.GetByName(CategoryNames.Entities);
        for(var s = 0; s < schemaCount; s++)
        {
            var schemaName = schemaNames[s];

            //the key is the table name. this has the list of columns that are computed or identity.
            var idenCompDic = StoreUtils.GetIdentityComputedGeneratedColumns(cInfo, schemaName, schemas[schemaName].Keys.ToArray());

            if(idenCompDic.Count < 1)
                continue;

            var tableNames = idenCompDic.Keys.ToArray();
            var tableCount = tableNames.Length;

            if(!cat.Schemas.Contains(schemaName))
                continue;

            var schema = cat.Schemas.GetByName(schemaName);
            for(var i = 0; i < tableCount; i++)
            {
                var tableName = tableNames[i];
                //entities are named from the cleaned table name (spaces, symbols...); look them up the same way
                var entityName = SF.Models.Utils.GenerateEntityName(tableName);

                //a table that was left out as not supported is not in the model
                if(entityName == null || !schema.Entities.Contains(entityName))
                    continue;

                var entity = schema.Entities.GetByName(entityName);
                var infoList = idenCompDic[tableName];

                foreach(var info in infoList)
                {
                    var hasIdentity = info.IsIdentity ? YesOrNo.Yes : YesOrNo.No;
                    var col = entity.Ps.GetByStoreName(info.Property);//info.Property is the column name in the database
                    col.Ide = hasIdentity;
                    col.IsComp = info.IsComputed ? YesOrNo.Yes : YesOrNo.No;
                    col.IsGen = info.IsGenerated ? YesOrNo.Yes : YesOrNo.No;

                    if(hasIdentity == YesOrNo.Yes)
                        entity.HId = YesOrNo.Yes;
                }
            }
        }
    }
    public static bool PopulateStoredProcsAndParams(Model model, Dictionary<string, Dictionary<string, DbObject>> schemas, List<string> notSupported = null)
    {
        var cInfo = model.ConInfo;
        var schemaNames = schemas.Keys.ToArray();
        var schemaCount = schemaNames.Length;
        var cat = model.Categories.GetByName(CategoryNames.Procs);
        var errorBuilder = new StringBuilder(15000);//warmup in case ther is any errors
        var now = DateTime.Now;
        errorBuilder.AppendLine($"Log started {now.ToLongDateString()} {now.ToLongTimeString()}");
        var errorCount = 0;

        for(var s = 0; s < schemaCount; s++)
        {
            var schema = schemaNames[s];
            var procs = schemas[schema];

            var dbObjectNames = procs.Keys.ToArray();
            var count = dbObjectNames.Length;

            var propsDic = StoreUtils.GetParamProperties(cInfo, schema, dbObjectNames);

            var newSchema = new Schema
            {
                Name = schema,
                Type = ModelObjectType.StoredProcs,
                StoredProcs = new StoredProcCollection()
            };

            for(var i = 0; i < count; i++)
            {
                var dbObject = procs[dbObjectNames[i]];

                var sp = new StoredProc
                {
                    SN = dbObject.Name,
                };

                if(!NameRules.IsValidIdentifier(sp.N))
                {
                    AddNotSupported(notSupported, "Stored procedures", schema, dbObject.Name, "the name cannot become a C# name");
                    continue;
                }

                var invalidParam = propsDic.ContainsKey(sp.SN) ? FirstInvalidName(propsDic[sp.SN]) : null;
                if(invalidParam != null)
                {
                    AddNotSupported(notSupported, "Stored procedures", schema, dbObject.Name, $"parameter '{invalidParam.SN}' cannot become a C# name");
                    continue;
                }

                if(propsDic.ContainsKey(sp.SN))
                {
                    sp.Par = new PropertyCollection(sp);
                    var props = propsDic[sp.SN];
                    foreach(var p in props)
                    {
                        sp.Par.Add(p);
                    }
                }

                errorCount += GetRoutineTableResultTypeAndAddToSchema(errorBuilder, cInfo, newSchema, ModelObjectType.StoredProcs, s: sp);

                var unsupportedResult = sp.En == null ? null : FirstUnsupportedColumnName(sp.En.Ps);
                var invalidResult = sp.En == null ? null : FirstInvalidName(sp.En.Ps);
                if(unsupportedResult != null)
                {
                    newSchema.StoredProcs.Remove(sp);
                    AddNotSupported(notSupported, "Stored procedures", schema, dbObject.Name, $"result column '{unsupportedResult.SN}' has characters that are not supported");
                }
                else if(invalidResult != null)
                {
                    newSchema.StoredProcs.Remove(sp);
                    AddNotSupported(notSupported, "Stored procedures", schema, dbObject.Name, $"result column '{invalidResult.SN}' cannot become a C# name");
                }
            }

            cat.Schemas.Add(newSchema);
        }

        return HandleProcessingErrors(errorBuilder, model, errorCount, ModelObjectType.StoredProcs);
    }
    public static bool PopulateFunctionsAndParams(Model model, Dictionary<string, Dictionary<string, DbObject>> schemas, List<string> notSupported = null)
    {
        var cInfo = model.ConInfo;
        var schemaNames = schemas.Keys.ToArray();
        var schemaCount = schemaNames.Length;
        var cat = model.Categories.GetByName(CategoryNames.Funcs);
        var errorBuilder = new StringBuilder(15000);//warmup in case ther is any errors
        var now = DateTime.Now;
        errorBuilder.AppendLine($"Log started {now.ToLongDateString()} {now.ToLongTimeString()}");
        var errorCount = 0;
        for(var s = 0; s < schemaCount; s++)
        {
            var schema = schemaNames[s];
            var funcs = schemas[schema];

            var dbObjectNames = funcs.Keys.ToArray();
            var count = dbObjectNames.Length;

            var propsDic = StoreUtils.GetParamProperties(cInfo, schema, dbObjectNames);
            var newSchema = new Schema
            {
                Name = schema,
                Type = ModelObjectType.Functions,
                Functions = new FunctionCollection()
            };

            for(var i = 0; i < count; i++)
            {
                var dbObject = funcs[dbObjectNames[i]];

                var func = new Function
                {
                    SN = dbObject.Name,
                    Sc = dbObject.Return,
                };

                if(!NameRules.IsValidIdentifier(func.N))
                {
                    AddNotSupported(notSupported, "Functions", schema, dbObject.Name, "the name cannot become a C# name");
                    continue;
                }

                var invalidParam = propsDic.ContainsKey(func.SN) ? FirstInvalidName(propsDic[func.SN]) : null;
                if(invalidParam != null)
                {
                    AddNotSupported(notSupported, "Functions", schema, dbObject.Name, $"parameter '{invalidParam.SN}' cannot become a C# name");
                    continue;
                }

                if(propsDic.ContainsKey(func.SN))
                {
                    func.Par = new PropertyCollection(func);
                    var props = propsDic[func.SN];
                    foreach(var p in props)
                    {
                        func.Par.Add(p);
                    }
                }

                if(dbObject.FunctionKind == FunctionKind.Table)//if it is a table valued function then get the entity to return
                {
                    errorCount += GetRoutineTableResultTypeAndAddToSchema(errorBuilder, cInfo, newSchema, ModelObjectType.Functions, f: func);

                    var unsupportedResult = func.En == null ? null : FirstUnsupportedColumnName(func.En.Ps);
                    var invalidResult = func.En == null ? null : FirstInvalidName(func.En.Ps);
                    if(unsupportedResult != null)
                    {
                        newSchema.Functions.Remove(func);
                        AddNotSupported(notSupported, "Functions", schema, dbObject.Name, $"result column '{unsupportedResult.SN}' has characters that are not supported");
                    }
                    else if(invalidResult != null)
                    {
                        newSchema.Functions.Remove(func);
                        AddNotSupported(notSupported, "Functions", schema, dbObject.Name, $"result column '{invalidResult.SN}' cannot become a C# name");
                    }
                }
                else
                    newSchema.Functions.Add(func);
            }

            cat.Schemas.Add(newSchema);
        }

        return HandleProcessingErrors(errorBuilder, model, errorCount, ModelObjectType.Functions);
    }
    public static bool PopulateSequences(Model model, Dictionary<string, Dictionary<string, DbObject>> schemas, List<string> notSupported = null)
    {
        var cInfo = model.ConInfo;
        var schemaNames = schemas.Keys.ToArray();
        var schemaCount = schemaNames.Length;
        var cat = model.Categories.GetByName(CategoryNames.Seqs);
        var errorBuilder = new StringBuilder(15000);//warmup in case ther is any errors
        var now = DateTime.Now;
        errorBuilder.AppendLine($"Log started {now.ToLongDateString()} {now.ToLongTimeString()}");
        var errorCount = 0;
        for(var s = 0; s < schemaCount; s++)
        {
            var schema = schemaNames[s];
            var seqs = schemas[schema];

            var dbObjectNames = seqs.Keys.ToArray();
            var count = dbObjectNames.Length;

            var newSchema = new Schema
            {
                Name = schema,
                Type = ModelObjectType.Sequences,
                Sequences = new SequenceCollection()
            };

            for(var i = 0; i < count; i++)
            {
                var dbObject = seqs[dbObjectNames[i]];

                var func = new Sequence
                {
                    SN = dbObject.Name,
                    Sc = dbObject.Return,
                };

                if(!NameRules.IsValidIdentifier(func.N))
                {
                    AddNotSupported(notSupported, "Sequences", schema, dbObject.Name, "the name cannot become a C# name");
                    continue;
                }

                newSchema.Sequences.Add(func);
            }

            cat.Schemas.Add(newSchema);
        }

        return HandleProcessingErrors(errorBuilder, model, errorCount, ModelObjectType.Sequences);
    }
    public static HashSet<string> GetSchemasToProcess(Model model)
    {
        var schemas = new HashSet<string>();
        var entCat = model.Categories.GetByName(CategoryNames.Entities);
        var viewCat = model.Categories.GetByName(CategoryNames.Views);
        var procCat = model.Categories.GetByName(CategoryNames.Procs);
        var funcCat = model.Categories.GetByName(CategoryNames.Funcs);
        var seqCat = model.Categories.GetByName(CategoryNames.Seqs);
        AddSchemasToProcess(entCat, schemas);
        AddSchemasToProcess(viewCat, schemas);
        AddSchemasToProcess(procCat, schemas);
        AddSchemasToProcess(funcCat, schemas);
        AddSchemasToProcess(seqCat, schemas);
        return schemas;
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
    private static bool HandleProcessingErrors(StringBuilder errorBuilder, Model model, int errorCount, ModelObjectType routineType)
    {
        if(errorCount < 1)
            return true;
        errorBuilder.AppendLine($"Error Count: {errorCount}");
        errorBuilder.AppendLine();
        IOUtils.WriteLog(model.Name, $"Errors{routineType}", errorBuilder.ToString());
        return false;
    }
    private static int GetRoutineTableResultTypeAndAddToSchema(StringBuilder errorBuilder, ConnectionInfo cInfo, Schema schema, ModelObjectType objectType, StoredProc s = null, Function f = null)
    {//returns 1 if there was an
        var routineNameForError = string.Empty;
        try
        {
            switch(objectType)
            {
                case ModelObjectType.StoredProcs:
                    routineNameForError = $"Stored Procedure {s.SN}";
                    s.En = StoreUtils.GetRoutineTableResultType(cInfo, schema.Name, s.SN, s.Count, false);
                    schema.StoredProcs.Add(s);
                    return 0;
                case ModelObjectType.Functions:
                    routineNameForError = $"Function {f.SN}";
                    f.En = StoreUtils.GetRoutineTableResultType(cInfo, schema.Name, f.SN, f.Count, true);
                    schema.Functions.Add(f);
                    return 0;
                default:
                    throw new ArgumentException($"ModelObjectType {objectType} not supported.");
            }
        }
        catch(SqlException sqlex)
        {
            errorBuilder.AppendLine("--------------------------------------------------------------------");
            errorBuilder.AppendLine($"Failed to determine the result type for {routineNameForError}.");
            errorBuilder.AppendLine("It will not be added to the model.");
            errorBuilder.AppendLine("Please fix the error in Sql Server:");
            errorBuilder.AppendLine(sqlex.Message);
            errorBuilder.AppendLine();
        }
        catch(Exception ex)
        {
            errorBuilder.AppendLine("--------------------------------------------------------------------");
            errorBuilder.AppendLine($"Failed to determine the result type for {routineNameForError}.");
            errorBuilder.AppendLine("It will not be added to the model.");
            errorBuilder.AppendLine("Please fix the error in Sql Server:");
            errorBuilder.AppendLine(ex.Message);
            errorBuilder.AppendLine();
        }

        return 1;//if it reached here it means there was an exception
    }
    private static void DetermineChildMultiplicity(FKey fk, Entity child)
    {
        //determine the multiplicity of the child.
        var multiplicitySingle = true;
        //first check if the primary key is involved in the FK
        //first check that he number of columns involved is the same in PK and the FK
        if(child.PK == null || child.PK.Ps == null || child.PK.Ps.Count != fk.FKParts.Count)
        {
            multiplicitySingle = false;
        }
        else
        {
            //now determine if all the columns in the FK child are present in the PK             
            foreach(var part in fk.FKParts)
            {
                if(!child.PK.Ps.Contains(part.CCol))
                {
                    multiplicitySingle = false;
                    break;
                }
            }
        }
        //if the FK does NOT match the PK then check the Unique Keys
        if(!multiplicitySingle && child.UKs != null && child.UKs.Count > 0)
        {
            foreach(var key in child.UKs)
            {
                if(key.Ps == null || key.Ps.Count != fk.FKParts.Count)
                    continue;

                var matches = true;
                foreach(var part in fk.FKParts)
                {
                    if(!key.Ps.Contains(part.CCol))
                    {
                        matches = false;
                        break;
                    }
                }

                if(matches)
                {
                    multiplicitySingle = true;
                    break;
                }
            }
        }

        if(multiplicitySingle)
            fk.CMu = "1";//by default this is set to multiple. so it is only set to single if it is determined.
    }
    private static bool IsFKSelfReferencing(Schema childSchema, Schema parentSchema, Entity child, Entity parent)
    {
        return string.CompareOrdinal(childSchema.Name, parentSchema.Name) == 0 && string.CompareOrdinal(child.N, parent.N) == 0;
    }
    private static void CreateNavPropertyOnEntity(Dictionary<string, int> repeatedProps, Entity ent, int fkId, string multi, string otherEnt, NavPropertyEnd end)
    {
        if(ent.NPs == null)
            ent.NPs = new NavPropertyCollection();

        var np = new NavProperty
        {
            FKId = fkId,
            Mul = multi,
            N = multi[0] == '1' ? otherEnt : Inflector.Pluralize(otherEnt),
            End = end,
        };

        //make the prop name unique. 
        //because there can be multiple foreign keys pointing to the same table. 
        //also if the property is named the same as the entity
        //also the nav property could end up with the same name of a simple property
        if(ent.NPs.Contains(np.N) || string.CompareOrdinal(np.N, ent.N) == 0 || (ent.Ps?.Count > 0 && ent.Ps.Contains(np.N)))
        {
            if(!repeatedProps.ContainsKey(np.N))
                repeatedProps.Add(np.N, 1);
            else
                repeatedProps[np.N]++;

            np.N += repeatedProps[np.N].ToString();
        }

        ent.NPs.Add(np);
    }
    private static void InitializeRepeatedNavPropForEntity(Dictionary<string, Dictionary<string, Dictionary<string, int>>> repeatedProps, string schemaName, string entName)
    {
        if(!repeatedProps.ContainsKey(schemaName))
        {
            repeatedProps.Add(schemaName, new Dictionary<string, Dictionary<string, int>>
            {
               { entName, new Dictionary<string, int>() },
            });
        }
        else if(!repeatedProps[schemaName].ContainsKey(entName))
        {
            repeatedProps[schemaName].Add(entName, new Dictionary<string, int>());
        }
    }
}

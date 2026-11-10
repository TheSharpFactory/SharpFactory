#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    public static string GenerateEntityInfoClass(ModelContainer modelContainer)
    {
        return EntityInfoClass.Generate(modelContainer);
    }
    private static class EntityInfoClass
    {
        internal static class NavPropertyInfoProperties
        {
            public static readonly string EntityId = "EntityId";
            public static readonly string IsParent = "IsParent";
            public static readonly string Predicate = "Predicate";
            public static readonly string NavProps = "NavProps";
        }
        internal static class NavPropertyPairProperties
        {
            //public static readonly string Prop = "Prop";
            //public static readonly string OtherProp = "OtherProp";
            public static readonly string PropId = "PropId";
            public static readonly string OtherPropId = "OtherPropId";
        }

        internal static class EntityInfoProperties
        {
            public static readonly string DbFQName = "DbFQName";
            public static readonly string Alias = "Alias";
            public static readonly string AsAlias = "AsAlias";
            public static readonly string Projection = "Projection";
            public static readonly string InsertProjection = "InsertProjection";
            public static readonly string InsertedProjection = "InsertedProjection";
            public static readonly string DeletedProjection = "DeletedProjection";
            public static readonly string Props = "Props";
        }
        internal static class PropertyInfoProperties
        {
            public static readonly string ParamName = "ParamName";
            public static readonly string Alias = "Alias";
            public static readonly string Column = "Column";
            public static readonly string SqlType = "SqlType";
            public static readonly string UdtTypeName = "UdtTypeName";
            public static readonly string Direction = "Direction";
            public static readonly string Size = "Size";
        }
        internal static class EntityQueryProperties
        {
            public static readonly string ModelId = "ModelId";
            public static readonly string EntityId = "EntityId";
            public static readonly string Top = "Top";
            public static readonly string Where = "Where";
            public static readonly string Changes = "Changes";
            public static readonly string Sort = "Sort";
            public static readonly string Pagination = "Pagination";
            public static readonly string NavProps = "NavProps";
        }
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeName = Common.Names.RepositoryEntityInfo;
            var typeNamespace = modelContainer.CommonLayers.Repository.FullNamespace(modelContainer.CommonSettings);
            //var typeDeclaration = $"public partial class {typeName}";
            var queryNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
            var usingNamespaces = new List<UsingNamespace>(3)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Data" },
              new UsingNamespace { Name = "System.Collections.Generic" },
              new UsingNamespace { Name = queryNamespace }
            };

            var summary = "Represents a Sql Table. This class is intended for internal use by the Repository classes for Sql Query Composition.";

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string> { summary }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(ArtifactKind.Class, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            PropertyExtensions.Auto(TypeInfo.String, EntityInfoProperties.DbFQName);
            PropertyExtensions.Auto(TypeInfo.String, EntityInfoProperties.Alias);
            PropertyExtensions.Auto(TypeInfo.String, EntityInfoProperties.AsAlias);
            PropertyExtensions.Auto(TypeInfo.String, EntityInfoProperties.Projection);
            PropertyExtensions.Auto(TypeInfo.String, EntityInfoProperties.InsertProjection);
            PropertyExtensions.Auto(TypeInfo.String, EntityInfoProperties.InsertedProjection);
            PropertyExtensions.Auto(TypeInfo.String, EntityInfoProperties.DeletedProjection);
            PropertyExtensions.Auto(new TypeInfo($"Dictionary<int, {Common.Names.RepositoryPropertyInfo}>"), EntityInfoProperties.Props);

            CSharpFile.CloseType();

            type.Name = Common.Names.RepositoryPropertyInfo;
            type.Summary[0] = "Represents a Sql Column. This class is intended for internal use by the Repository classes for Sql Query Composition.";

            CSharpFile.OpenClass(type);

            PropertyExtensions.Auto(TypeInfo.String, PropertyInfoProperties.Column);
            PropertyExtensions.Auto(TypeInfo.String, PropertyInfoProperties.Alias);
            PropertyExtensions.Auto(TypeInfo.String, PropertyInfoProperties.ParamName);
            PropertyExtensions.Auto(new TypeInfo("SqlDbType"), PropertyInfoProperties.SqlType);
            PropertyExtensions.Auto(TypeInfo.String, PropertyInfoProperties.UdtTypeName);
            PropertyExtensions.Auto(new TypeInfo("ParameterDirection"), PropertyInfoProperties.Direction, defaultValue: "ParameterDirection.Input");
            PropertyExtensions.Auto(TypeInfo.IntNullable, PropertyInfoProperties.Size);

            CSharpFile.CloseType();

            type.Name = Common.Names.RepositoryEntityQuery;
            type.Summary[0] = "Represents a Sql Query. This class is intended for internal use by the Repository classes for Sql Query Composition.";

            CSharpFile.OpenClass(type);

            var filterListType = new GenericParameter { Name = "IQueryFilter" };
            var sortFilterListType = new GenericParameter { Name = "ISortFilter" };

            PropertyExtensions.Auto(TypeInfo.Int, EntityQueryProperties.ModelId);
            PropertyExtensions.Auto(TypeInfo.Int, EntityQueryProperties.EntityId);
            PropertyExtensions.Auto(TypeInfo.Int, EntityQueryProperties.Top);
            PropertyExtensions.Auto(new TypeInfo(filterListType, false, false, true), EntityQueryProperties.Where);
            PropertyExtensions.Auto(new TypeInfo(filterListType, false, false, true), EntityQueryProperties.Changes);
            PropertyExtensions.Auto(new TypeInfo(sortFilterListType, false, false, true), EntityQueryProperties.Sort);
            PropertyExtensions.Auto(new TypeInfo("NavPropertyInfo", false, false, true), EntityQueryProperties.NavProps);
            PropertyExtensions.Auto(new TypeInfo("IPaginationSettings"), EntityQueryProperties.Pagination);

            CSharpFile.CloseType();

            type.Name = Common.Names.RepositoryNavPropertyInfo;
            type.Summary[0] = "Represents a Sql Foreign Key. This class is intended for internal use by the Repository classes for Sql Query Composition.";

            CSharpFile.OpenClass(type);
            PropertyExtensions.Auto(TypeInfo.Int, NavPropertyInfoProperties.EntityId);
            PropertyExtensions.Auto(TypeInfo.Bool, NavPropertyInfoProperties.IsParent);
            PropertyExtensions.Auto(new TypeInfo(Common.Names.RepositoryNavPropertyPair, isList: true), NavPropertyInfoProperties.Predicate);
            PropertyExtensions.Auto(new TypeInfo(type.Name, isList: true), NavPropertyInfoProperties.NavProps);

            var cloneMethod = new MethodDeclaration
            {
                Name = "Clone",
                AccessModifier = AccessModifiers.Public,
                ReturnType = new TypeInfo(type.Name),
            };
            CSharpFile.DeclareMethod(cloneMethod);
            CSharpFile.OpenBlock();
            CSharpFile.LineNoEnding($"var result = new {cloneMethod.ReturnType}");
            CSharpFile.OpenBlock();
            CSharpFile.LineNoEnding("EntityId = EntityId,");
            CSharpFile.LineNoEnding("IsParent = IsParent");
            CSharpFile.CloseBlockWithSemicolon();

            CSharpFile.If("Predicate?.Count > 0");

            CSharpFile.OpenBlock();
            CSharpFile.Line($"result.Predicate = new List<{Common.Names.RepositoryNavPropertyPair}>(Predicate.Count)");

            CSharpFile.ForEach("nvp", "Predicate");
            CSharpFile.OpenBlock();
            CSharpFile.Line("result.Predicate.Add(nvp.Clone())");
            CSharpFile.CloseBlock();

            CSharpFile.CloseBlock();

            CSharpFile.If("NavProps?.Count > 0");

            CSharpFile.OpenBlock();
            CSharpFile.Line($"result.NavProps = new List<{Common.Names.RepositoryNavPropertyInfo}>(NavProps.Count)");

            CSharpFile.ForEach("nvi", "NavProps");
            CSharpFile.OpenBlock();
            CSharpFile.Line("result.NavProps.Add(nvi.Clone())");
            CSharpFile.CloseBlock();

            CSharpFile.CloseBlock();

            CSharpFile.Return("result");
            CSharpFile.CloseBlock();
            CSharpFile.CloseType();

            type.Name = Common.Names.RepositoryNavPropertyPair;
            type.Summary[0] = "Represents a Sql Foreign Key Predicate. This class is intended for internal use by the Repository classes for Sql Query Composition.";

            CSharpFile.OpenClass(type);
            PropertyExtensions.Auto(TypeInfo.Int, NavPropertyPairProperties.PropId);
            PropertyExtensions.Auto(TypeInfo.Int, NavPropertyPairProperties.OtherPropId);

            cloneMethod = new MethodDeclaration
            {
                Name = "Clone",
                AccessModifier = AccessModifiers.Public,
                ReturnType = new TypeInfo(type.Name),
            };
            CSharpFile.DeclareMethod(cloneMethod);
            CSharpFile.OpenBlock();
            CSharpFile.LineNoEnding($"var result = new {cloneMethod.ReturnType}");
            CSharpFile.OpenBlock();
            CSharpFile.LineNoEnding("PropId = PropId,");
            CSharpFile.LineNoEnding("OtherPropId = OtherPropId");
            CSharpFile.CloseBlockWithSemicolon();
            CSharpFile.Return("result");
            CSharpFile.CloseBlock();

            //CSharpFile.CloseBlock();
            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }

        public static void GenerateDefaultSortingMembersForRepositoryClass(Entity entity)
        {
            var ukCount = 0;
            if(entity.UKs?.Count > 0)
                ukCount = entity.UKs.Count;

            var containerName = Common.GeneratePropClassName(entity);
            var sortCollection = $"List<{Common.Names.ISortFilter}>";
            var sortFilter = Common.Names.SortFilter;

            CSharpFile.OpenRegion("Default Sorting");

            if(entity.PK != null && entity.PK.Ps != null && entity.PK.Ps.Count > 0)//if primary key found. use this as default sorting for the table
            {
                #region PK As Default Sort        
                CSharpFile.Comment($"this is the PK ({entity.PK.N}) sorting. used as default for method overloads that DO NOT take a List<ISortFilter> parameter");
                CSharpFile.AppendIndented($"{DefaultSortField} = new {sortCollection}({entity.PK.Ps.Count}){{ ");
                foreach(var p in entity.PK.Ps)
                    CSharpFile.Append($"{sortFilter}.New({containerName}.{p.N}), ");
                CSharpFile.CloseLine("}");
                #endregion
            }
            else
            {
                if(ukCount > 0)//use the first unique key found as default
                {
                    #region UK As Default Sort
                    if(entity.UKs[0].Ps != null && entity.UKs[0].Ps.Count > 0)
                    {
                        var propCount = entity.UKs[0].Ps.Count;
                        CSharpFile.Comment($"this is the UK({entity.UKs[0].N}) sorting. used as default for method overloads that DO NOT take a List<ISortFilter> parameter");
                        CSharpFile.AppendIndented($"{DefaultSortField} = new {sortCollection}({entity.UKs[0].Ps.Count}){{ ");
                        for(var i = 0; i < propCount; i++)
                            CSharpFile.Append($"{sortFilter}.New({containerName}.{entity.UKs[0].Ps[i].N}), ");
                        CSharpFile.CloseLine("}");
                    }
                    #endregion
                }
                else//else use the id sort as default
                {
                    var foundId = false;
                    #region ID As Default Sort
                    if(entity.Ps?.Count > 0)
                    {
                        foreach(var p in entity.Ps)
                        {
                            if(p.Ide == YesOrNo.Yes)
                            {
                                foundId = true;
                                CSharpFile.Comment("this is the Identity sorting. used as default for method overloads that DO NOT take a List<ISortFilter> parameter");
                                CSharpFile.Line($"{DefaultSortField} = new {sortCollection}(1){{ {sortFilter}.New({containerName}.{p.N}) }}");
                            }
                        }
                    }

                    #endregion
                    //no sorting found at all so just set a null value
                    if(!foundId)
                    {
                        CSharpFile.Comment("there was no default sort found in the store. keep this variable as a placeholder.");
                        CSharpFile.Comment("it is also possible to add a constructor and fill this collection with a custom sort.");
                        CSharpFile.Line($"{DefaultSortField} = new {sortCollection}(0)");
                    }
                }
            }

            CSharpFile.CloseRegion();
        }
        public static void GenerateAdditionalSortingMembersForRepositoryClass(Entity entity)
        {
            if(!(entity.UKs?.Count > 0))
                return;

            var containerName = Common.GeneratePropClassName(entity);
            var sortCollection = $"List<{Common.Names.ISortFilter}>";
            var sortFilter = Common.Names.SortFilter;

            CSharpFile.OpenRegion("Unique Key Sorting");

            var keyCount = 0;
            foreach(var uk in entity.UKs)
            {
                if(!(uk.Ps?.Count > 0))
                    continue;

                keyCount++;

                CSharpFile.Comment($"this is the UK({uk.N}) sorting.");
                CSharpFile.AppendIndented($"private static readonly {sortCollection} {GenerateSortByFieldName(uk)} = new {sortCollection}({uk.Ps.Count}){{ ");
                foreach(var p in uk.Ps)
                    CSharpFile.Append($"{sortFilter}.New({containerName}.{p.N}), ");
                CSharpFile.CloseLine("}");
            }

            CSharpFile.CloseRegion();
        }
        public static void GenerateEntityInfoInstanceForRepositoryLookup(Model model, Entity entity, Schema schema)
        {
            var entityInfoName = Common.Names.RepositoryEntityInfo;
            var aliasRaw = IdGenerator.NextId();
            var aliasValue = $"[{aliasRaw}]";

            CSharpFile.AppendIndented($"{{{Common.Names.RepositoryLookupEntityId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, schema.Name, entity.N)}, new {entityInfoName}{{");
            CSharpFile.Append($"{EntityInfoProperties.DbFQName} = \"[{schema.Name}].[{entity.SN}]\",");
            CSharpFile.Append($" {EntityInfoProperties.Alias} = \"{aliasValue}\",");
            CSharpFile.Append($" {EntityInfoProperties.AsAlias} = \"[{schema.Name}].[{entity.SN}] AS {aliasValue}\",");
            CSharpFile.Append($" {EntityInfoProperties.Projection} = {GenerateProjection(entity.Ps, aliasValue)},");
            CSharpFile.Append($" {EntityInfoProperties.InsertProjection} = {GenerateInsertProjection(entity.Ps)},");
            CSharpFile.Append($" {EntityInfoProperties.InsertedProjection} = {GenerateInsertedProjection(entity.Ps)},");//this is used to return updated rows from an update statement as well as from an insert
            CSharpFile.Append($" {EntityInfoProperties.DeletedProjection} = {GenerateDeletedProjection(entity.Ps)},");//this is used to return deleted rows from a delete statement or the "before" and update statement

            if(entity.Ps?.Count > 0)
            {
                var propEnumType = model.Name + "E." + schema.Name + "." + Common.GeneratePropEnumName(entity);
                var propCount = entity.Ps.Count;
                CSharpFile.Append($" {EntityInfoProperties.Props} = new Dictionary<int, {Common.Names.RepositoryPropertyInfo}>({propCount}){{");
                foreach(var p in entity.Ps)
                {
                    CSharpFile.Append($"{{(int){propEnumType}.{p.N}, new {Common.Names.RepositoryPropertyInfo}{{");
                    CSharpFile.Append($"{PropertyInfoProperties.Column} = \"[{p.SN}]\",");
                    CSharpFile.Append($" {PropertyInfoProperties.Alias} = \"{aliasValue}.[{p.SN}]\",");
                    CSharpFile.Append($" {PropertyInfoProperties.ParamName} = \"@{aliasRaw}_{p.SN}\",");
                    var sqlType = Lookup.TypeInfo[p.TId].SqlDbType;
                    CSharpFile.Append($" {PropertyInfoProperties.SqlType} = SqlDbType.{sqlType},");
                    if(p.Udt)
                    {
                        CSharpFile.Append($" {PropertyInfoProperties.UdtTypeName} = \"{p.ST}\",");
                    }

                    CSharpFile.Append($" {PropertyInfoProperties.Size} = {(p.S.HasValue ? p.S.Value.ToString() : "null")},");
                    CSharpFile.Append("}},");
                }

                CSharpFile.Append("},");
            }
            else
            {
                CSharpFile.Append($" {EntityInfoProperties.Props} = new Dictionary<int, {Common.Names.RepositoryPropertyInfo}>(0),");
            }
            //b.Append($" {EntityInfoProperties.PropCount} = {propCount},");
            CSharpFile.Append(" }},");
            CSharpFile.EmptyLine();
        }
        public static void GenerateStoredProcInfoInstanceForRepositoryLookup(Model model, StoredProc proc, Schema schema)
        {
            var entityInfoName = Common.Names.RepositoryEntityInfo;
            CSharpFile.AppendIndented($"{{{Common.Names.RepositoryLookupStoredProcId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, schema.Name, proc.N)}, new {entityInfoName}{{");
            CSharpFile.Append($"{EntityInfoProperties.DbFQName} = \"[{schema.Name}].[{proc.SN}]\",");

            if(proc.Par?.Count > 0)
            {
                var propEnumType = model.Name + "E." + schema.Name + "." + Common.GeneratePropEnumName(proc);
                var propCount = proc.Par.Count;
                CSharpFile.Append($" {EntityInfoProperties.Props} = new Dictionary<int, {Common.Names.RepositoryPropertyInfo}>({propCount}){{");
                foreach(var p in proc.Par)
                {
                    CSharpFile.Append($"{{(int){propEnumType}.{p.N}, new {Common.Names.RepositoryPropertyInfo}{{");
                    CSharpFile.Append($" {PropertyInfoProperties.ParamName} = \"{p.SN}\",");
                    CSharpFile.Append($" {PropertyInfoProperties.SqlType} = SqlDbType.{Lookup.TypeInfo[p.TId].SqlDbType},");
                    CSharpFile.Append($" {PropertyInfoProperties.Size} = {(p.S.HasValue ? p.S.Value.ToString() : "null")},");
                    if(p.M.HasValue)
                    {
                        if(p.M.Value == ParamMode.InOut)
                        {
                            CSharpFile.Append($" {PropertyInfoProperties.Direction} = ParameterDirection.InputOutput,");
                        }
                        else if(p.M.Value == ParamMode.Out)
                        {
                            CSharpFile.Append($" {PropertyInfoProperties.Direction} = ParameterDirection.Output ,");
                        }
                    }

                    CSharpFile.Append("}},");
                }

                CSharpFile.Append("},");
            }
            else
            {
                CSharpFile.Append($" {EntityInfoProperties.Props} = new Dictionary<int, {Common.Names.RepositoryPropertyInfo}>(0),");
            }
            //b.Append($" {EntityInfoProperties.PropCount} = {propCount},");
            CSharpFile.Append("}},");
            CSharpFile.EmptyLine();
        }
        public static void GenerateFunctionInfoInstanceForRepositoryLookup(Model model, Function entity, Schema schema)
        {
            var entityInfoName = Common.Names.RepositoryEntityInfo;

            CSharpFile.AppendIndented($"{{{Common.Names.RepositoryLookupFunctionId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, schema.Name, entity.N)}, new {entityInfoName}{{");
            CSharpFile.Append($"{EntityInfoProperties.DbFQName} = \"[{schema.Name}].[{entity.SN}]\",");
            CSharpFile.Append("}},");
            CSharpFile.EmptyLine();
        }
        public static void GenerateSequenceInfoInstanceForRepositoryLookup(Model model, Sequence seq, Schema schema)
        {
            var entityInfoName = Common.Names.RepositoryEntityInfo;
            CSharpFile.AppendIndented($"{{{Common.Names.RepositoryLookupSequenceId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, schema.Name, seq.N)}, new {entityInfoName}{{");
            CSharpFile.Append($"{EntityInfoProperties.DbFQName} = \"[{schema.Name}].[{seq.SN}]\",");
            CSharpFile.Append(" }},");
            CSharpFile.EmptyLine();

        }
        private static string GenerateProjection(PropertyCollection props, string aliasField)
        {
            if(props == null || props.Count < 1)
                return "null";

            var result = $"\"{aliasField}.[{props[0].SN}]";//do the first one to avoid trimming the last comma 
            var count = props.Count;

            if(count > 1)
            {
                for(var i = 1; i < count; i++)
                    result += $",{aliasField}.[{props[i].SN}]";//do the rest with comma prefixed
            }

            result += "\"";
            return result;
        }
        private static string GenerateInsertedProjection(PropertyCollection props)
        {
            return GenerateCRUDProjection(props, "INSERTED");
        }
        private static string GenerateDeletedProjection(PropertyCollection props)
        {
            return GenerateCRUDProjection(props, "DELETED");
        }
        private static string GenerateCRUDProjection(PropertyCollection props, string sqloperation)//sqloperation can be INSERTED,DELETED
        {
            if(props == null || props.Count < 1)
                return "null";

            var result = $"\"[{sqloperation}].[{props[0].SN}]";//do the first one to avoid trimming the last comma 
            var count = props.Count;

            if(count > 1)
            {
                for(var i = 1; i < count; i++)
                    result += $",[{sqloperation}].[{props[i].SN}]";//do the rest with comma prefixed
            }

            result += "\"";
            return result;
        }
        private static string GenerateInsertProjection(PropertyCollection props)
        {
            if(props == null || props.Count < 1)
                return "null";

            var result = string.Empty;
            var count = props.Count;
            if(count > 0)
            {
                var isFirstDone = false;
                for(var i = 0; i < count; i++)
                {
                    if(props[i].RO == YesOrNo.Yes)
                        continue;

                    if(isFirstDone)
                    {
                        result += $",[{props[i].SN}]";//do the rest with comma prefixed              
                    }
                    else
                    {
                        result += $"\"[{props[i].SN}]";//do the first one to avoid trimming the last comma 
                        isFirstDone = true;
                    }
                }
            }

            if(result.Length == 0)
                return "null";

            result += "\"";
            return result;
        }
    }
}

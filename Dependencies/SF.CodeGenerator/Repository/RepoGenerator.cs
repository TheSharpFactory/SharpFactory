#region Usings
using System.Collections.ObjectModel;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    private sealed class KeyInfo
    {
        public bool IsPK { get; set; }
        public bool IsNavPropTheParent { get; set; }
        public string KeyTypes { get; set; }
        public string KeyPropCalls { get; set; }
        public string OtherEntityFullName { get; set; }
        public Key Key { get; set; }
        public FKey FK { get; set; }
        public Entity Entity { get; set; }
        public Schema OtherSchema { get; set; }
        public Entity OtherEntity { get; set; }
        public Entity KeyEntity { get; set; }
        public string KeyEntityFullName { get; set; }
        public NavProperty NavProp { get; set; }
    }

    #region Common Helpers
    private static void MaterializeProperty(Property p, int index)
    {
        if(p.Xml)
        {
            CSharpFile.LineNoEnding($"{p.N} = {RepoMethodNames.MaterializeXmlProperty}(r, {index}),");
        }
        else if(p.Geog || p.Geom)
        {
            CSharpFile.LineNoEnding($"{p.N} = {p.T}.Deserialize(r.{Lookup.TypeInfo[p.TId].ReaderMethod}({index})),");
        }
        else if(p.T.LastIndexOf(']') > -1)
        {
            CSharpFile.LineNoEnding($"{p.N} = r.GetValue({index}) as {p.T},");
        }
        else if(p.Nul == YesOrNo.Yes)
        {
            if(p.Str || string.CompareOrdinal(p.T, "object") == 0)
                CSharpFile.LineNoEnding($"{p.N} = r.GetValue({index}) as {p.T},");
            else
                CSharpFile.LineNoEnding($"{p.N} = r.IsDBNull({index}) ? null : r.{Lookup.TypeInfo[p.TId].ReaderMethod}({index}),");//the typed read: GetValue boxes every value of every row
        }
        else
        {
            CSharpFile.LineNoEnding($"{p.N} = r.{Lookup.TypeInfo[p.TId].ReaderMethod}({index}),");
        }
    }
    private static string GenerateSortByFieldName(Key uk)
    {
        return $"_sortBy_{uk.N}";
    }
    private static string GenerateMaterializeInlineFunction(bool supportsNavProps, bool supportsPagination, string listCapacity)
    {
        var materializer = supportsNavProps ? ", materializers" : $", {RepoMethodNames.MaterializeEntity}";
        var pagination = supportsPagination ? ", pagination" : "";

        return $"void materialize (SqlDataReader r) {{ {RepoMethodNames.MaterializeResult}(r{materializer}, resultSets{pagination}{listCapacity}); }}";
    }
    #endregion

    #region Common Routine Helpers
    private static void GenerateRoutineMaterializer(Entity returnEntity)
    {
        CSharpFile.Line($"var result = new List<{returnEntity.N}>(500)", "starts at 500 rows and grows from there: a call can return a few rows or thousands");

        CSharpFile.LineNoEnding("void materialize(SqlDataReader r)");

        CSharpFile.OpenBlock();
        CSharpFile.OpenRegion("Materializer");
        CSharpFile.While("r.Read()");
        CSharpFile.OpenBlock();
        CSharpFile.LineNoEnding($"result.Add(new {returnEntity.N}");
        CSharpFile.OpenBlock();

        var props = returnEntity.Ps;
        if(props?.Count > 0)
        {
            var propCount = returnEntity.Ps.Count;
            for(var i = 0; i < propCount; i++)
            {
                MaterializeProperty(props[i], i);
            }
        }

        CSharpFile.CloseBlockWithParenthesis();
        CSharpFile.CloseBlock();
        CSharpFile.CloseRegion();

        CSharpFile.CloseBlock();

    }
    private static void GenerateRoutineScalarMaterializer(Property scalarProp)
    {
        var returnType = !scalarProp.Str ? scalarProp.T + "?" : scalarProp.T;

        CSharpFile.Line($"{returnType} result = null;", "make it nullable.  functions can return null.");
        CSharpFile.LineNoEnding("void materialize (SqlDataReader r)");

        CSharpFile.OpenBlock();
        CSharpFile.OpenRegion("Materializer");
        CSharpFile.If("r.Read()");
        if(scalarProp.Str)
            CSharpFile.LineIndented($"result = r.GetValue(0) as {returnType}");
        else
            CSharpFile.LineIndented($"result = r.IsDBNull(0) ? null : r.{Lookup.TypeInfo[scalarProp.TId].ReaderMethod}(0)");
        CSharpFile.CloseRegion();

        CSharpFile.CloseBlock();
    }
    #endregion

    private static class Helpers
    {
        public static string GenerateKeysForSelectSingle(Entity entity, string queryFilter, string containerName, string filterConditionEnum, string entityParam)
        {
            if(entity.PK?.Ps?.Count > 0)//if the primary key was found use it          
                return GenerateKeysForSingleSelectHelper(entity.PK.Ps, queryFilter, containerName, filterConditionEnum, entityParam);

            if(entity.UKs?.Count > 0 && entity.UKs[0].Ps?.Count > 0)//if no primary key found use a UK            
                return GenerateKeysForSingleSelectHelper(entity.UKs[0].Ps, queryFilter, containerName, filterConditionEnum, entityParam);

            if(entity.HId == YesOrNo.Yes)//if no primary key and no uk found use the identity
            {
                var keys = string.Empty;
                var props = entity.Ps;
                var propCount = props.Count;
                for(var i = 0; i < propCount; i++)
                {
                    var p = props[i];
                    if(p.Ide == YesOrNo.Yes)
                    {
                        keys += $"{queryFilter}.New({containerName}.{p.N}, {filterConditionEnum}.Equals, {entityParam}.{p.N})";
                        break;
                    }
                }

                return keys;
            }

            //if not PK, UK or ID found use all the properties. this is for heap tables.          
            if(entity.Ps?.Count > 0)
                return GenerateKeysForSingleSelectHelper(entity.Ps, queryFilter, containerName, filterConditionEnum, entityParam);

            return string.Empty;
        }
        private static string GenerateKeysForSingleSelectHelper<TModelMember>(ObservableCollection<TModelMember> props, string queryFilter, string containerName, string filterConditionEnum, string entityParam) where TModelMember : IModelMember
        {
            var keys = $"{queryFilter}.New({containerName}.{props[0].N}, {filterConditionEnum}.Equals, {entityParam}.{props[0].N})";
            var propCount = props.Count;
            for(var i = 1; i < propCount; i++)
            {
                keys += $", {queryFilter}.New({containerName}.{props[i].N}, {filterConditionEnum}.Equals, {entityParam}.{props[i].N})";
            }

            return keys;
        }

        public static void GenerateSelectCall(bool isAsync, bool returnSingle, bool supportsNavProps, bool hasNavProps, bool defaultSort, string defaultSortBy, bool supportsPagination, bool supportsFilter, bool supportsTop)
        {
            if(!returnSingle)
            {
                CSharpFile.Line(GenerateSelectManyCall(isAsync, supportsNavProps, hasNavProps, defaultSort, defaultSortBy, supportsPagination, supportsFilter, supportsTop, true));
            }
            else
            {
                CSharpFile.Line(GenerateSelectSingleCall(isAsync, supportsNavProps, hasNavProps, defaultSort, defaultSortBy, supportsFilter));
            }
        }
        public static string GenerateSelectManyCall(bool isAsync, bool supportsNavProps, bool hasNavProps, bool defaultSort, string defaultSortBy, bool supportsPagination, bool supportsFilter, bool supportsTop, bool returnsValue)
        {
            var top = supportsTop ? RepoMethodSignatures.ParamNames._topCount : "0";
            var queryFilters = supportsFilter ? RepoMethodSignatures.ParamNames._queryFilters : "null";
            var sortFilters = defaultSort ? defaultSortBy : RepoMethodSignatures.ParamNames._sortFilters;
            var pagination = supportsPagination ? RepoMethodSignatures.ParamNames._paginationSettings : "null";
            var navProps = hasNavProps ? RepoMethodSignatures.ParamNames._navProps : "null";
            var returns = ComposeReturnStatementPart(isAsync, returnsValue);
            var methodNameSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.SelectMany}{methodNameSuffix}";

            if(supportsNavProps)
            {
                if(hasNavProps)
                    return $"{returns}{methodName}({queryFilters}, {sortFilters}, {navProps}, {pagination}, {top})";
                else
                    return $"{returns}{methodName}({queryFilters}, {sortFilters}, {pagination}, {top})";
            }
            else
            {
                return $"{returns}{methodName}({queryFilters}, {sortFilters}, {pagination}, {top})";
            }
        }
        //The select calls these helpers write are the whole body of the method (nothing before or after them that needs
        //the result), so an async method returns the inner Task instead of awaiting it: declare it with skipAwait.
        private static string ComposeReturnStatementPart(bool isAsync, bool returnsValue)
        {
            if(isAsync)
                return returnsValue ? "return " : "await ";

            return returnsValue ? "return " : "";
        }
        public static string GenerateSelectManyCallBasedOnQuery(bool isAsync, bool supportsNavProps, bool supportsPagination, bool returnsValue, int top)
        {
            var pagination = supportsPagination ? "query.Prepare().Pagination" : "null";
            var topCount = top < 0 ? "topcount" : top.ToString();
            var returns = ComposeReturnStatementPart(isAsync, returnsValue);
            var methodName = isAsync ? $"{RepoMethodNames.SelectQuery}Async" : RepoMethodNames.SelectQuery;

            return $"{returns}{methodName}(query.Prepare(), {pagination}, {topCount})";
        }
        public static string GenerateSelectManyCallNoQuery(bool isAsync, bool supportsPagination, bool returnsValue, int top)
        {
            var pagination = supportsPagination ? "query.Prepare().Pagination" : "null";
            var topCount = top < 0 ? "topcount" : top.ToString();
            var returns = ComposeReturnStatementPart(isAsync, returnsValue);
            var methodNameSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.SelectMany}{methodNameSuffix}";

            return $"{returns}{methodName}(null, {DefaultSortField}, {pagination}, {topCount})";
        }
        public static string GenerateSelectSingleCall(bool isAsync, bool supportsNavProps, bool hasNavProps, bool defaultSort, string defaultSortBy, bool supportsFilter)
        {
            var queryFilters = supportsFilter ? RepoMethodSignatures.ParamNames._queryFilters : "null";
            var sortFilters = defaultSort ? defaultSortBy : RepoMethodSignatures.ParamNames._sortFilters;
            var navProps = hasNavProps ? RepoMethodSignatures.ParamNames._navProps : "null";
            var methodNameSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.SelectSingle}{methodNameSuffix}";
            var returns = ComposeReturnStatementPart(isAsync, true);

            if(supportsNavProps)
            {
                if(hasNavProps)
                    return $"{returns}{methodName}({queryFilters}, {sortFilters}, {navProps});";
                else
                    return $"{returns}{methodName}({queryFilters}, {sortFilters});";
            }
            else
            {
                return $"{returns}{methodName}({queryFilters}, {sortFilters});";
            }
        }
    }
}

#region Usings
#endregion

namespace SF.CodeGeneration;

public static partial class Common
{
    public static class Names
    {
        public static string SequenceRepository { get; } = "SequenceRepository";
        public static string ISequenceRepository { get; } = "ISequenceRepository";
        public static string StoredProcRepository { get; } = "StoredProcRepository";
        public static string IStoredProcRepository { get; } = "IStoredProcRepository";
        public static string FunctionRepository { get; } = "FunctionRepository";
        public static string IFunctionRepository { get; } = "IFunctionRepository";
        public static string RepositoryContainer { get; } = "RepositoryContainer";
        public static string IRepositoryContainer { get; } = "IRepositoryContainer";
        public static string ModelRepositoryContainer { get; } = "ModelRepositoryContainer";
        public static string IModelRepositoryContainer { get; } = "IModelRepositoryContainer";
        public static string SchemaRepositoryContainer { get; } = "SchemaRepositoryContainer";
        public static string ISchemaRepositoryContainer { get; } = "ISchemaRepositoryContainer";
        public static string RepositoryBase { get; } = "RepositoryBase";
        public static string StoredProcRepositoryBase { get; } = "StoredProcedureRepositoryBase";
        public static string FuncRepositoryBase { get; } = "FunctionRepositoryBase";
        public static string SeqRepositoryBase { get; } = "SequenceRepositoryBase";
        public static string ReadableRepositoryBase { get; } = "ReadableRepositoryBase";
        public static string ReadableNavRepositoryBase { get; } = "ReadableNavRepositoryBase";
        public static string WritableRepositoryBase { get; } = "WritableRepositoryBase";
        public static string WritableNavRepositoryBase { get; } = "WritableNavRepositoryBase";
        public static string MultiKeyDictionary { get; } = "MultiKeyDictionary";
        public static string RepositoryLookup { get; } = "RepoLookup";
        public static string RepositoryLookupModelId { get; } = "ModelId";
        public static string RepositoryLookupEntityId { get; } = "EntityId";
        //public static string RepositoryLookupViewId { get; } = "ViewId";
        public static string RepositoryLookupStoredProcId { get; } = "StoredProcId";
        public static string RepositoryLookupSequenceId { get; } = "SequenceId";
        public static string RepositoryLookupFunctionId { get; } = "FunctionId";
        //public static string RepositoryLookupModelName { get; } = "ModelName";
        public static string RepositoryEntityInfo { get; } = "EntityInfo";
        public static string RepositoryEntityQuery { get; } = "EntityQuery";
        public static string RepositoryPropertyInfo { get; } = "PropertyInfo";
        public static string RepositoryNavPropertyInfo { get; } = "NavPropertyInfo";
        public static string RepositoryNavPropertyPair { get; } = "NavPropertyPair";
        //public static string IQuery { get; } = "IQuery";
        public static string IQueryFilter { get; } = "IQueryFilter";
        public static string ISortFilter { get; } = "ISortFilter";
        public static string QueryFilter { get; } = "QueryFilter";
        public static string BatchEntry { get; } = "BatchEntry";
        public static string FilterBuilder { get; } = "FilterBuilder";
        public static string ExpressionBuilder { get; } = "ExpressionBuilder";
        public static string UpdateValuesBuilder { get; } = "UpdateValuesBuilder";
        public static string PaginationBuilder { get; } = "PaginationBuilder";
        public static string QueryBuilder { get; } = "QueryBuilder";
        public static string PredicateBuilder { get; } = "PredicateBuilder";
        public static string SortBuilder { get; } = "SortBuilder";
        public static string NavPropsBuilder { get; } = "NavPropsBuilder";
        public static string NavPathBuilder { get; } = "NavPathBuilder";
        public static string Query { get; } = "Query";
        public static string QueryWrapper { get; } = "QueryWrapper";
        public static string Compare { get; } = "Compare";
        public static string FilterConditionsEnum { get; } = "FilterConditions";
        public static string FilterOperatorsEnum { get; } = "FilterOperators";
        public static string SortFilter { get; } = "SortFilter";
        public static string NavProp { get; } = "NavProp";
        public static string NavPropOfContainer { get; } = "NavPropOfContainer";
        public static string ContainerNavProp { get; } = "ContainerNavProp";
        public static string Prop { get; } = "Prop";
        public static string PropertyContainer { get; } = "PropertyContainer";
        public static string NavPropertyContainer { get; } = "NavPropertyContainer";
        public static string PropertyFilter { get; } = "PropertyFilter";
        public static string PropertySortFilter { get; } = "PropertySortFilter";
        public static string FilterBuilderExtensions { get; } = "FilterBuilderExtensions";
        public static string NavPropQueryExtensions { get; } = "NavPropQueryExtensions";
        public static string NavPropCollection { get; } = "NavProps";
        public static string SortDirectionEnum { get; } = "SortDirection";
        public static string PaginationSettings { get; } = "PaginationSettings";
        public static string IPaginationSettings { get; } = "IPaginationSettings";
        public static string StoredProcResult { get; } = "StoredProcResult";
        public static string DataExchangeException { get; } = "DataExchangeException";

        public static string GenericEntity { get; } = "TEntity";
        public static string GenericProp { get; } = "TProps";
        public static string GenericValue { get; } = "TValue";
    }
}

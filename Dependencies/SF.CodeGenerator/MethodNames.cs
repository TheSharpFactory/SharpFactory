namespace SF.CodeGeneration;

internal static class EntityUtilsMethodNames
{
    public static string HasChanges { get; } = "HasChanges";
    public static string Merge { get; } = "Merge";
    public static string GetChanges { get; } = "GetChanges";
    public static string ByteArrayEquals { get; } = "ByteArrays";
}
internal static class QueryMethodNames
{
    //API Surface
    //public static string AddNavProp { get; } = "NavProp";
    public static string AddSubNavProp { get; } = "SubNavProp";
}
internal static class RepoMethodNames
{
    //API Surface
    public static string GetById { get; } = "ById";
    public static string GetByPK { get; } = "ByPK";
    public static string GetByUK { get; } = "ByUK";
    public static string GetList { get; } = "ToList";
    public static string GetTop { get; } = "Top";
    public static string GetFirst { get; } = "First";
    public static string GetFirstOrDefault { get; } = "FirstOrDefault";
    public static string GetSingle { get; } = "Single";
    public static string GetSingleOrDefault { get; } = "SingleOrDefault";
    public static string Create { get; } = "Create";
    public static string Update { get; } = "Update";
    public static string UpdateOrCreate { get; } = "UpdateOrCreate";
    public static string DeleteById { get; } = "DeleteById";
    public static string DeleteByPK { get; } = "DeleteByPK";
    public static string DeleteByUK { get; } = "DeleteByUK";
    public static string Delete { get; } = "Delete";

    //Repository Private
    public static string StoredProcedure { get; } = "StoredProcedure";
    public static string Sequence { get; } = "Sequence";
    public static string Function { get; } = "Function";
    public static string SelectSingle { get; } = "SelectSingle";
    public static string SelectMany { get; } = "SelectMany";
    public static string SelectQuery { get; } = "SelectQuery";
    public static string InsertEntity { get; } = "InsertEntity";
    //public static string DeleteEntity { get; } = "DeleteEntity";
    public static string DeleteAny { get; } = "DeleteAny";
    public static string UpdateEntity { get; } = "UpdateEntity";
    public static string UpdateAny { get; } = "UpdateAny";
    public static string UpdateOptimisticEntity { get; } = "UpdateOptimisticEntity";
    public static string UpdateOptimisticBatch { get; } = "UpdateOptimisticBatch";
    public static string UpdateOrCreateEntity { get; } = "UpdateOrCreateEntity";
    public static string MaterializeResult { get; } = "MaterializeResult";
    public static string CalculateListCapacity { get; } = "CalculateListCapacity";
    public static string Materialize { get; } = "Materialize";
    public static string MaterializeEntity { get; } = "MaterializeEntity";
    public static string UnboxEntityList { get; } = "UnboxEntityList";
    public static string AddEntityToList { get; } = "AddEntityToList";
    public static string MaterializeXmlProperty { get; } = "MaterializeXmlProperty";
    public static string MaterializeEntityResult { get; } = "MaterializeEntityResult";
    public static string MaterializeSingleEntity { get; } = "MaterializeSingleEntity";
    public static string GetNavPropMaterializers { get; } = "GetNavPropMaterializers";
    public static string ComposeNavPropMaterializers { get; } = "ComposeNavPropMaterializers";
    public static string BuildNavPropInfos { get; } = "BuildNavPropInfos";
    public static string BuildNavPropInfoTree { get; } = "BuildNavPropInfoTree";
    public static string BuildEntityTree { get; } = "BuildEntityTree";
    public static string ComposeKeys { get; } = "ComposeKeys";
    public static string ComposeInsertPredicate { get; } = "ComposeInsertPredicate";
    public static string GetChanges { get; } = "GetChanges";
    public static string Merge { get; } = "Merge";
    public static string BuildObjectGraph { get; } = "BuildObjectGraph";
    public static string PopulateNavProperties { get; } = "PopulateNavProperties";
    public static string ComposeDictionaryByPK { get; } = "ComposeDictionaryByPK";
    public static string ComposeDictionaryByUK { get; } = "ComposeDictionaryByUK";

    //private static readonly string _dataBaseUpdateOptimistic = "UpdateOptimistic";

    //Database Surface
    public static string DatabaseSelect { get; } = "Select";
    public static string DatabaseInsert { get; } = "Insert";
    public static string DatabaseDelete { get; } = "Delete";
    public static string DatabaseUpdate { get; } = "Update";
    public static string DatabaseStoredProc { get; } = "StoredProc";
    public static string DatabaseSequence { get; } = "Sequence";
    public static string DatabaseFunction { get; } = "Function";
}

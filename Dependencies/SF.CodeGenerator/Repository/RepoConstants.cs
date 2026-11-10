#region Usings
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    internal static string SqlEngineName { get; } = "Database";
    private static string ModelIdField { get; } = "ModelId";
    private static string EntityIdField { get; } = "EntityId";
    private static string NavPropInfosField { get; } = "_navPropInfos";
    private static string DefaultSortField { get; } = "DefaultSort";
    private static string HasTriggersField { get; } = "HasTriggers";
    private static string PredicateFiltersProp { get; } = "PredicateFilters";
}

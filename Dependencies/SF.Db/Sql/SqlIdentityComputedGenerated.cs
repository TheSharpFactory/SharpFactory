namespace SF.Db;

public static class SqlIdentityComputedGenerated
{
    public static class Params
    {
        public static string Table { get; } = "TABLE_NAME";
        public static string Column { get; } = "COLUMN_NAME";
        public static string IsIdentity { get; } = "IS_IDENTITY";
        public static string IsComputed { get; } = "IS_COMPUTED";
        public static string IsGenerated { get; } = "IS_GENERATED";
    }
}

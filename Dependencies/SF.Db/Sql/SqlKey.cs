namespace SF.Db;

public static class SqlKey
{
    public static class Params
    {
        public static string TableName { get; } = "TABLE_NAME";
        public static string Name { get; } = "CONSTRAINT_NAME";
        public static string Ordinal { get; } = "ORDINAL_POSITION";
        public static string ColumnName { get; } = "COLUMN_NAME";
        public static string Type { get; } = "CONSTRAINT_TYPE";
    }
}

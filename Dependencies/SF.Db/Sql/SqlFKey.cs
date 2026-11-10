namespace SF.Db;

public static class SqlFKey
{
    public static class Params
    {
        public static string FKName { get; } = "FK_CONSTRAINT_NAME";
        public static string ChildTable { get; } = "FK_TABLE_NAME";
        public static string ChildCol { get; } = "FK_COLUMN_NAME";
        public static string FKPartOrdinal { get; } = "FK_ORDINAL_POSITION";
        public static string ParentKey { get; } = "UQ_CONSTRAINT_NAME";
        public static string ParentSchema { get; } = "UQ_CONSTRAINT_SCHEMA";
        public static string ParentTable { get; } = "UQ_TABLE_NAME";
        public static string ParentCol { get; } = "UQ_COLUMN_NAME";
    }
}

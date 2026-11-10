namespace SF.Db;

public static class SqlTrigger
{
    public static class Params
    {
        public static string TableSchema { get; } = "TABLE_SCHEMA";
        public static string TableName { get; } = "TABLE_NAME";
        public static string TriggerName { get; } = "TRIGGER_NAME";
    }
}

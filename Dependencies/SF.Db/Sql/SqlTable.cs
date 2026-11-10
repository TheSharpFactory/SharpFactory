namespace SF.Db;

// Schema Restrictions: https://msdn.microsoft.com/en-us/library/cc716722(v=vs.110).aspx
// Schema Collections: https://msdn.microsoft.com/en-us/library/ms254969.aspx
// Samples: https://msdn.microsoft.com/en-us/library/ms136366(v=vs.110).aspx

public static class SqlTable
{
    public static class Params
    {
        //public static string Catalog { get; } = "TABLE_CATALOG";//not being used in the select
        public static string Schema { get; } = "TABLE_SCHEMA";
        public static string Name { get; } = "TABLE_NAME";
        //public static string Type { get; } = "TABLE_TYPE";//not being used in the select
    }
    public static string TypeTable { get; } = "BASE TABLE";

    public static string TypeView { get; } = "VIEW";
}

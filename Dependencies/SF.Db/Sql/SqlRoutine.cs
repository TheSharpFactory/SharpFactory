namespace SF.Db;

// Schema Restrictions: https://msdn.microsoft.com/en-us/library/cc716722(v=vs.110).aspx
// Schema Collections: https://msdn.microsoft.com/en-us/library/ms254969.aspx
// Samples: https://msdn.microsoft.com/en-us/library/ms136366(v=vs.110).aspx

public static class SqlRoutine
{
    public static class Params
    {
        //public static string Catalog { get; } = "SPECIFIC_CATALOG"; not being used in the select
        public static string Schema { get; } = "SPECIFIC_SCHEMA";
        public static string Name { get; } = "SPECIFIC_NAME";
        //public static string Type { get; } = "ROUTINE_TYPE";not being used in the select
    }
    public static string TypeStoredProc { get; } = "PROCEDURE";
    public static string TypeFunction { get; } = "FUNCTION";
}

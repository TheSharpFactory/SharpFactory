namespace SF.Models;

public static class KeyTypes
{
    public static string PK { get; } = "PRIMARY KEY";
    public static string UK { get; } = "UNIQUE";
}

public static class CategoryNames
{
    public const string Entities = "Entities";
    public const string Views = "Views";
    public const string Procs = "StoredProcs";
    public const string Funcs = "Functions";
    public const string Seqs = "Sequences";
}
public static class DbCategoryNames
{
    public const string Entities = "Tables";
    public const string Views = "Views";
    public const string Procs = "StoredProcs";
    public const string Funcs = "Functions";
    public const string Seqs = "Sequences";
}

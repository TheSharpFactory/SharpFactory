namespace SF.Db;

// Schema Restrictions: https://msdn.microsoft.com/en-us/library/cc716722(v=vs.110).aspx
// Schema Collections: https://msdn.microsoft.com/en-us/library/ms254969.aspx
// Samples: https://msdn.microsoft.com/en-us/library/ms136366(v=vs.110).aspx

/// <summary>
/// These classes represents SqlServer constants and interactions.   
/// In the future a MySql class and other providers will be added.
/// </summary>

public static class SqlColumn
{
    public static class Params
    {
        public static string Catalog { get; } = "TABLE_CATALOG";
        public static string Schema { get; } = "TABLE_SCHEMA";
        public static string TableName { get; } = "TABLE_NAME";
        public static string Name { get; } = "COLUMN_NAME";
        public static string Ordinal { get; } = "ORDINAL_POSITION";
        public static string DefaultValue { get; } = "COLUMN_DEFAULT";
        public static string IsNullable { get; } = "IS_NULLABLE";
        public static string DataType { get; } = "DATA_TYPE";
        public static string MaxStringLength { get; } = "CHARACTER_MAXIMUM_LENGTH";
        public static string MaxBytesInString { get; } = "CHARACTER_OCTET_LENGTH";
        public static string NumberPrecision { get; } = "NUMERIC_PRECISION";//total number of digits to be stored
        public static string NumberPrecisionRadix { get; } = "NUMERIC_PRECISION_RADIX";// it is 10 for int, money and decimal and 2 for float & real. this means 10 to the power of 10 or 2 to the power of 53
        public static string NumberScale { get; } = "NUMERIC_SCALE";//total number of decimal places to store
        public static string DateTimePrecision { get; } = "DATETIME_PRECISION";
        public static string CharSetCatalog { get; } = "CHARACTER_SET_CATALOG";
        public static string CharSetSchema { get; } = "CHARACTER_SET_SCHEMA";
        public static string CharSetName { get; } = "CHARACTER_SET_NAME";
        public static string CollationCatalog { get; } = "COLLATION_CATALOG";
        public static string CollationSchema { get; } = "COLLATION_SCHEMA";
        public static string CollationName { get; } = "COLLATION_NAME";
        public static string DomainCatalog { get; } = "DOMAIN_CATALOG";
        public static string DomainSchema { get; } = "DOMAIN_SCHEMA";
        public static string DomainName { get; } = "DOMAIN_NAME";

    }
}
//public class SqlColumRestrictions
//{
//  public string Catalog { get; set; } = null;
//  public string Schema { get; set; } = null;
//  public string TableName { get; set; } = null;
//  public string Name { get; set; } = null;
//  public string[] Value
//  {
//    get
//    {
//      return new string[] { Catalog, Schema, TableName, Name };
//    }
//  }
//}

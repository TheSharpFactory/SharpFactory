namespace SF.Db;

//this class represents one row returned when calling SqlDataReader.GetTableSchema https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldatareader.getschematable(v=vs.110).aspx
/// <summary>
/// These classes represents SqlServer constants and interactions.   
/// In the future a MySql class and other providers will be added.
/// </summary>

public static class SqlSchemaColumn
{
    public static class Params
    {
        public static string IsNullable { get; } = "AllowDBNull";
        public static string Catalog { get; } = "BaseCatalogName";
        public static string BaseColumn { get; } = "BaseColumnName";
        public static string BaseSchema { get; } = "BaseSchemaName";
        public static string BaseServer { get; } = "BaseServerName";
        public static string BaseTable { get; } = "BaseTableName";
        public static string Name { get; } = "ColumnName";
        public static string Ordinal { get; } = "ColumnOrdinal";
        public static string Size { get; } = "ColumnSize";
        public static string DataType { get; } = "DataTypeName";
        public static string IsAliased { get; } = "IsAliased";
        public static string IsAutoIncrement { get; } = "IsAutoIncrement";
        public static string IsColumnSet { get; } = "IsColumnSet";
        public static string IsExpression { get; } = "IsExpression";
        public static string IsHidden { get; } = "IsHidden";
        public static string IsIdentity { get; } = "IsIdentity";
        public static string IsKey { get; } = "IsKey";
        public static string IsBLOB { get; } = "IsLong";
        public static string IsReadOnly { get; } = "IsReadOnly";
        public static string IsRowVersion { get; } = "IsRowVersion";
        public static string IsUnique { get; } = "IsUnique";
        public static string NonVersionedProviderType { get; } = "NonVersionedProviderType";
        public static string NumberPrecision { get; } = "NumericPrecision";
        public static string NumberScale { get; } = "NumericScale";
        public static string ProviderSpecificDataType { get; } = "ProviderSpecificDataType";
        public static string ProviderType { get; } = "ProviderType";
        public static string UdtAssemblyQualifiedName { get; } = "UdtAssemblyQualifiedName";
        public static string XmlSchemaCollectionDatabase { get; } = "XmlSchemaCollectionDatabase";
        public static string XmlSchemaCollectionName { get; } = "XmlSchemaCollectionName";
        public static string XmlSchemaCollectionOwningSchema { get; } = "XmlSchemaCollectionOwningSchema";
    }
}

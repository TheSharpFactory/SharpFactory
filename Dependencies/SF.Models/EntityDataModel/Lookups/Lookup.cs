#region Usings
using System.Collections.Generic;
#endregion

namespace SF.Models;

public static class Lookup
{
    // SqlDbType Enum: https://msdn.microsoft.com/en-us/library/system.data.sqldbtype(v=vs.110).aspx
    // Linq To SQL mappings http://stackoverflow.com/questions/18606493/do-entity-framework-provide-utility-that-convert-db-type-to-c-sharp-type
    //entity framework maps SqlDbType.Real to Double which seems incorrect
    //concerning geograpy and geometry: http://stackoverflow.com/questions/25316503/how-to-read-a-geography-column-by-sqldatareader
    //more about geography and geometry http://stackoverflow.com/questions/17218050/passing-in-geography-type-and-sql-converts-to-geography-type
    //more about geography and geometry http://stackoverflow.com/questions/3736666/sqldbtype-and-geography
    //more about geography and geometry https://ask.sqlservercentral.com/questions/19717/reading-geography-data-type-using-sqldatareader-in.html
    //TODO: Implement geography, geometry
    //TODO: Test SqlDbType.Real columns to see if they work normally.
    //TODO: Implement xml. There is a link next to the xml
    //XML implementation http://stackoverflow.com/questions/1698153/calling-a-stored-procedure-with-xml-datatype
    #region SqlMetal.EXE code
    //this code is from the internal implementation of SqlMetal.exe (Linq To SQL)
    //internal static Type GetClosestRuntimeType(SqlDbType sqlDbType)
    //{
    //  switch(sqlDbType)
    //  {
    //    case SqlDbType.BigInt:
    //      return typeof(long);

    //    case SqlDbType.Binary:
    //    case SqlDbType.Image:
    //    case SqlDbType.Timestamp:
    //    case SqlDbType.VarBinary:
    //      return typeof(Binary);

    //    case SqlDbType.Bit:
    //      return typeof(bool);

    //    case SqlDbType.Char:
    //    case SqlDbType.NChar:
    //    case SqlDbType.NText:
    //    case SqlDbType.NVarChar:
    //    case SqlDbType.Text:
    //    case SqlDbType.VarChar:
    //      return typeof(string);

    //    case SqlDbType.DateTime:
    //    case SqlDbType.SmallDateTime:
    //    case SqlDbType.Date:
    //    case SqlDbType.DateTime2:
    //      return typeof(DateTime);

    //    case SqlDbType.Decimal:
    //    case SqlDbType.Money:
    //    case SqlDbType.SmallMoney:
    //      return typeof(decimal);

    //    case SqlDbType.Float:
    //      return typeof(double);

    //    case SqlDbType.Int:
    //      return typeof(int);

    //    case SqlDbType.Real:
    //      return typeof(float);

    //    case SqlDbType.UniqueIdentifier:
    //      return typeof(Guid);

    //    case SqlDbType.SmallInt:
    //      return typeof(short);

    //    case SqlDbType.TinyInt:
    //      return typeof(byte);

    //    case SqlDbType.Xml:
    //      return typeof(XElement);

    //    case SqlDbType.Time:
    //      return typeof(TimeSpan);

    //    case SqlDbType.DateTimeOffset:
    //      return typeof(DateTimeOffset);
    //  }
    //  return typeof(object);
    //}
    #endregion
    public static readonly Dictionary<int, TypeMappingInfo> TypeInfo = new Dictionary<int, TypeMappingInfo>(68)
    {
     #region TypeMappings
		  {1, new TypeMappingInfo {NetType = "long", StoreType ="bigint", SqlDbType = "BigInt", ReaderMethod="GetInt64"  }},
  {2, new TypeMappingInfo {NetType = "byte[]", StoreType ="binary", SqlDbType = "Binary", ReaderMethod = "GetBytes"}},
  {3, new TypeMappingInfo {NetType = "bool", StoreType ="bit", SqlDbType = "Bit", ReaderMethod="GetBoolean"}},
  {4, new TypeMappingInfo {NetType = "string", StoreType ="char", SqlDbType = "Char", ReaderMethod="GetString"}},
  {5, new TypeMappingInfo {NetType = "DateTime", StoreType ="date", SqlDbType = "Date", ReaderMethod="GetDateTime"}},
  {6, new TypeMappingInfo {NetType = "DateTime", StoreType ="datetime", SqlDbType = "DateTime", ReaderMethod="GetDateTime"}},
  {7, new TypeMappingInfo {NetType = "DateTime", StoreType ="datetime2", SqlDbType = "DateTime2", ReaderMethod="GetDateTime"}},
  {8, new TypeMappingInfo {NetType = "DateTimeOffset", StoreType ="datetimeoffset", SqlDbType = "DateTimeOffset", ReaderMethod="GetDateTimeOffset"}},
  {9, new TypeMappingInfo {NetType = "decimal", StoreType ="decimal", SqlDbType = "Decimal", ReaderMethod="GetDecimal"}},
  {10, new TypeMappingInfo {NetType = "double", StoreType ="float", SqlDbType = "Float", ReaderMethod="GetDouble"}},
  {11, new TypeMappingInfo {NetType = "string", StoreType ="hierarchyid", SqlDbType = "NVarChar", ReaderMethod="GetString"}},
  {12, new TypeMappingInfo {NetType = "byte[]", StoreType ="image", SqlDbType = "Binary", ReaderMethod="GetBytes"}},
  {13, new TypeMappingInfo {NetType = "int", StoreType ="int", SqlDbType = "Int", ReaderMethod="GetInt32"}},
  {14, new TypeMappingInfo {NetType = "decimal", StoreType ="money", SqlDbType = "Money", ReaderMethod="GetDecimal"}},
  {15, new TypeMappingInfo {NetType = "string", StoreType ="nchar", SqlDbType = "NChar", ReaderMethod="GetString"}},
  {16, new TypeMappingInfo {NetType = "string", StoreType ="ntext", SqlDbType = "NText", ReaderMethod="GetString"}},
  {17, new TypeMappingInfo {NetType = "decimal", StoreType ="numeric", SqlDbType = "Decimal", ReaderMethod="GetDecimal"}},
  {18, new TypeMappingInfo {NetType = "string", StoreType ="nvarchar", SqlDbType = "NVarChar", ReaderMethod="GetString"}},
  {19, new TypeMappingInfo {NetType = "float", StoreType ="real", SqlDbType = "Real", ReaderMethod="GetFloat"}},
  {20, new TypeMappingInfo {NetType = "byte[]", StoreType ="rowversion", SqlDbType = "Timestamp", ReaderMethod="GetBytes"}},
  {21, new TypeMappingInfo {NetType = "DateTime", StoreType ="smalldatetime", SqlDbType = "DateTime", ReaderMethod="GetDateTime"}},
  {22, new TypeMappingInfo {NetType = "short", StoreType ="smallint", SqlDbType = "SmallInt", ReaderMethod="GetInt16"}},
  {23, new TypeMappingInfo {NetType = "decimal", StoreType ="smallmoney", SqlDbType = "SmallMoney", ReaderMethod="GetDecimal"}},
  {24, new TypeMappingInfo {NetType = "object", StoreType ="sql_variant", SqlDbType = "Variant", ReaderMethod="GetValue"}},
  {25, new TypeMappingInfo {NetType = "string", StoreType ="sysname", SqlDbType = "NVarChar", ReaderMethod="GetString"}},
  {26, new TypeMappingInfo {NetType = "string", StoreType ="text", SqlDbType = "Text", ReaderMethod="GetString"}},
  {27, new TypeMappingInfo {NetType = "TimeSpan", StoreType ="time", SqlDbType = "Time", ReaderMethod="GetTimeSpan"}},//TODO: Test this. NOTES: with 24 hours cycle seems to be the way to make it work
  {28, new TypeMappingInfo {NetType = "byte[]", StoreType ="timestamp", SqlDbType = "Timestamp", ReaderMethod="GetBytes"}},
  {29, new TypeMappingInfo {NetType = "byte", StoreType ="tinyint", SqlDbType = "TinyInt", ReaderMethod="GetByte"}},
  {30, new TypeMappingInfo {NetType = "Guid", StoreType ="uniqueidentifier", SqlDbType = "UniqueIdentifier", ReaderMethod="GetGuid"}},
  {31, new TypeMappingInfo {NetType = "byte[]", StoreType ="varbinary", SqlDbType = "VarBinary", ReaderMethod="GetBytes"}},
  {32, new TypeMappingInfo {NetType = "string", StoreType ="varchar", SqlDbType = "VarChar", ReaderMethod="GetString"}},
  //TODO: this might need to be adjusted  .NET Core https://docs.microsoft.com/en-us/ef/core/modeling/spatial
  {33, new TypeMappingInfo {NetType = "Microsoft.SqlServer.Types.SqlGeography", StoreType ="geography", SqlDbType = "Udt", ReaderMethod="GetSqlBytes"}},
  {34, new TypeMappingInfo {NetType = "Microsoft.SqlServer.Types.SqlGeometry", StoreType ="geometry", SqlDbType = "Udt", ReaderMethod="GetSqlBytes"}},
  {35, new TypeMappingInfo {NetType = "XDocument", StoreType ="xml", SqlDbType = "Xml", ReaderMethod="GetSqlXml"}},
	  #endregion
    };
    public static readonly Dictionary<string, int> TypeIds = new Dictionary<string, int>(68)
    {
      #region TypeMappings
		  {"bigint", 1},
  {"binary", 2},
  {"bit", 3},
  {"char", 4},
  {"date", 5},
  {"datetime", 6},
  {"datetime2", 7},
  {"datetimeoffset", 8},
  {"decimal", 9},
  {"float", 10},
  {"hierarchyid", 11},
  {"image", 12},
  {"int", 13},
  {"money", 14},
  {"nchar", 15},
  {"ntext", 16},
  {"numeric", 17},
  {"nvarchar", 18},
  {"real", 19},
  {"rowversion", 20},
  {"smalldatetime", 21},
  {"smallint", 22},
  {"smallmoney", 23},
  {"sql_variant", 24},
  {"sysname", 25},
  {"text", 26},
  {"time", 27},//with 24 hours cycle
  {"timestamp", 28},
  {"tinyint", 29},
  {"uniqueidentifier", 30},
  {"varbinary", 31},
  {"varchar", 32},
  {"geography",33 },
  {"geometry",34 },
  {"xml", 35 }
	    #endregion
    };

    public static readonly HashSet<string> BinaryStoreTypes = new HashSet<string>
    {
      "binary",
      "varbinary",
      "image",
      "rowversion",
      "timestamp"
    };

    public static readonly HashSet<string> AutoComputedTypes = new HashSet<string>
    {
      "rowversion",
      "timestamp"
    };
    public static int GetTypeId(string storeType)
    {
        var key = storeType.ToLower();

        if(TypeIds.ContainsKey(key))
            return TypeIds[key];

        return -1;
    }
}

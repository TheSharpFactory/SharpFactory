namespace SF.CodeGeneration;

public sealed class TypeInfo
{
    public string Name { get; }
    public bool IsNullable { get; }
    public bool IsArray { get; }
    public bool IsList { get; }

    public bool IsVoid { get; }
    public bool IsTask { get; }

    public TypeInfo(GenericParameter param, bool isNullable = false, bool isArray = false, bool isList = false)
    {
        Name = param.Name;
        IsNullable = isNullable;
        IsArray = isArray;
        IsList = isList;
    }
    public TypeInfo(string name, bool isNullable = false, bool isArray = false, bool isList = false)
    {
        Name = name;
        IsNullable = isNullable;
        IsArray = isArray;
        IsList = isList;
        IsTask = string.CompareOrdinal("Task", name) == 0;
        IsVoid = string.CompareOrdinal("void", name) == 0;
    }

    public static readonly TypeInfo Void = new TypeInfo("void", false, false, false);
    public static readonly TypeInfo Task = new TypeInfo("Task", false, false, false);
    public static readonly TypeInfo SByte = new TypeInfo("sbyte", false, false, false);
    public static readonly TypeInfo Byte = new TypeInfo("byte", false, false, false);
    public static readonly TypeInfo Short = new TypeInfo("short", false, false, false);
    public static readonly TypeInfo UShort = new TypeInfo("ushort", false, false, false);
    public static readonly TypeInfo Int = new TypeInfo("int", false, false, false);
    public static readonly TypeInfo Uint = new TypeInfo("uint", false, false, false);
    public static readonly TypeInfo Long = new TypeInfo("long", false, false, false);
    public static readonly TypeInfo ULong = new TypeInfo("ulong", false, false, false);
    public static readonly TypeInfo Float = new TypeInfo("float", false, false, false);
    public static readonly TypeInfo Double = new TypeInfo("double", false, false, false);
    public static readonly TypeInfo Decimal = new TypeInfo("decimal", false, false, false);
    public static readonly TypeInfo String = new TypeInfo("string", false, false, false);
    public static readonly TypeInfo Bool = new TypeInfo("bool", false, false, false);
    public static readonly TypeInfo DateTime = new TypeInfo("DateTime", false, false, false);
    public static readonly TypeInfo DateTimeOffset = new TypeInfo("DateTimeOffset", false, false, false);
    public static readonly TypeInfo TimeSpan = new TypeInfo("TimeSpan", false, false, false);
    public static readonly TypeInfo Object = new TypeInfo("object", false, false, false);
    public static readonly TypeInfo Guid = new TypeInfo("Guid", false, false, false);
    public static readonly TypeInfo Type = new TypeInfo("Type", false, false, false);
    public static readonly TypeInfo TypeCode = new TypeInfo("TypeCode", false, false, false);
    //TODO: this might need to be adjusted  .NET Core https://docs.microsoft.com/en-us/ef/core/modeling/spatial
    public static readonly TypeInfo SqlGeography = new TypeInfo("Microsoft.SqlServer.Types.SqlGeography", false, false, false);
    public static readonly TypeInfo SqlGeometry = new TypeInfo("Microsoft.SqlServer.Types.SqlGeometry", false, false, false);

    public static readonly TypeInfo SByteArray = new TypeInfo("sbyte", false, true, false);
    public static readonly TypeInfo ByteArray = new TypeInfo("byte", false, true, false);
    public static readonly TypeInfo ShortArray = new TypeInfo("short", false, true, false);
    public static readonly TypeInfo UShortArray = new TypeInfo("ushort", false, true, false);
    public static readonly TypeInfo IntArray = new TypeInfo("int", false, true, false);
    public static readonly TypeInfo UintArray = new TypeInfo("uint", false, true, false);
    public static readonly TypeInfo LongArray = new TypeInfo("long", false, true, false);
    public static readonly TypeInfo ULongArray = new TypeInfo("ulong", false, true, false);
    public static readonly TypeInfo FloatArray = new TypeInfo("float", false, true, false);
    public static readonly TypeInfo DoubleArray = new TypeInfo("double", false, true, false);
    public static readonly TypeInfo DecimalArray = new TypeInfo("decimal", false, true, false);
    public static readonly TypeInfo StringArray = new TypeInfo("string", false, true, false);
    public static readonly TypeInfo BoolArray = new TypeInfo("bool", false, true, false);
    public static readonly TypeInfo DateTimeArray = new TypeInfo("DateTime", false, true, false);
    public static readonly TypeInfo DateTimeOffsetArray = new TypeInfo("DateTimeOffset", false, true, false);
    public static readonly TypeInfo TimeSpanArray = new TypeInfo("TimeSpan", false, true, false);
    public static readonly TypeInfo ObjectArray = new TypeInfo("object", false, true, false);
    public static readonly TypeInfo GuidArray = new TypeInfo("Guid", false, true, false);
    public static readonly TypeInfo TypeArray = new TypeInfo("Type", false, true, false);
    public static readonly TypeInfo TypeCodeArray = new TypeInfo("TypeCode", false, true, false);
    //TODO: this might need to be adjusted  .NET Core https://docs.microsoft.com/en-us/ef/core/modeling/spatial
    public static readonly TypeInfo SqlGeographyArray = new TypeInfo("Microsoft.SqlServer.Types.SqlGeography", false, true, false);
    public static readonly TypeInfo SqlGeometryArray = new TypeInfo("Microsoft.SqlServer.Types.SqlGeometry", false, true, false);

    public static readonly TypeInfo SByteList = new TypeInfo("sbyte", false, false, true);
    public static readonly TypeInfo ByteList = new TypeInfo("byte", false, false, true);
    public static readonly TypeInfo ShortList = new TypeInfo("short", false, false, true);
    public static readonly TypeInfo UShortList = new TypeInfo("ushort", false, false, true);
    public static readonly TypeInfo IntList = new TypeInfo("int", false, false, true);
    public static readonly TypeInfo UintList = new TypeInfo("uint", false, false, true);
    public static readonly TypeInfo LongList = new TypeInfo("long", false, false, true);
    public static readonly TypeInfo ULongList = new TypeInfo("ulong", false, false, true);
    public static readonly TypeInfo FloatList = new TypeInfo("float", false, false, true);
    public static readonly TypeInfo DoubleList = new TypeInfo("double", false, false, true);
    public static readonly TypeInfo DecimalList = new TypeInfo("decimal", false, false, true);
    public static readonly TypeInfo StringList = new TypeInfo("string", false, false, true);
    public static readonly TypeInfo BoolList = new TypeInfo("bool", false, false, true);
    public static readonly TypeInfo DateTimeList = new TypeInfo("DateTime", false, false, true);
    public static readonly TypeInfo DateTimeOffsetList = new TypeInfo("DateTimeOffset", false, false, true);
    public static readonly TypeInfo TimeSpanList = new TypeInfo("TimeSpan", false, false, true);
    public static readonly TypeInfo ObjectList = new TypeInfo("object", false, false, true);
    public static readonly TypeInfo GuidList = new TypeInfo("Guid", false, false, true);
    public static readonly TypeInfo TypeList = new TypeInfo("Type", false, false, true);
    public static readonly TypeInfo TypeCodeList = new TypeInfo("TypeCode", false, false, true);
    //TODO: this might need to be adjusted  .NET Core https://docs.microsoft.com/en-us/ef/core/modeling/spatial
    public static readonly TypeInfo SqlGeographyList = new TypeInfo("Microsoft.SqlServer.Types.SqlGeography", false, false, true);
    public static readonly TypeInfo SqlGeometryList = new TypeInfo("Microsoft.SqlServer.Types.SqlGeometry", false, false, true);

    public static readonly TypeInfo SByteNullable = new TypeInfo("sbyte", true, false, false);
    public static readonly TypeInfo ByteNullable = new TypeInfo("byte", true, false, false);
    public static readonly TypeInfo ShortNullable = new TypeInfo("short", true, false, false);
    public static readonly TypeInfo UShortNullable = new TypeInfo("ushort", true, false, false);
    public static readonly TypeInfo IntNullable = new TypeInfo("int", true, false, false);
    public static readonly TypeInfo UintNullable = new TypeInfo("uint", true, false, false);
    public static readonly TypeInfo LongNullable = new TypeInfo("long", true, false, false);
    public static readonly TypeInfo ULongNullable = new TypeInfo("ulong", true, false, false);
    public static readonly TypeInfo FloatNullable = new TypeInfo("float", true, false, false);
    public static readonly TypeInfo DoubleNullable = new TypeInfo("double", true, false, false);
    public static readonly TypeInfo DecimalNullable = new TypeInfo("decimal", true, false, false);
    public static readonly TypeInfo BoolNullable = new TypeInfo("bool", true, false, false);
    public static readonly TypeInfo DateTimeNullable = new TypeInfo("DateTime", true, false, false);
    public static readonly TypeInfo DateTimeOffsetNullable = new TypeInfo("DateTimeOffset", true, false, false);
    public static readonly TypeInfo TimeSpanNullable = new TypeInfo("TimeSpan", true, false, false);
    public static readonly TypeInfo GuidNullable = new TypeInfo("Guid", true, false, false);

    public static readonly TypeInfo SByteNullableArray = new TypeInfo("sbyte", true, true, false);
    public static readonly TypeInfo ByteNullableArray = new TypeInfo("byte", true, true, false);
    public static readonly TypeInfo ShortNullableArray = new TypeInfo("short", true, true, false);
    public static readonly TypeInfo UShortNullableArray = new TypeInfo("ushort", true, true, false);
    public static readonly TypeInfo IntNullableArray = new TypeInfo("int", true, true, false);
    public static readonly TypeInfo UintNullableArray = new TypeInfo("uint", true, true, false);
    public static readonly TypeInfo LongNullableArray = new TypeInfo("long", true, true, false);
    public static readonly TypeInfo ULongNullableArray = new TypeInfo("ulong", true, true, false);
    public static readonly TypeInfo FloatNullableArray = new TypeInfo("float", true, true, false);
    public static readonly TypeInfo DoubleNullableArray = new TypeInfo("double", true, true, false);
    public static readonly TypeInfo DecimalNullableArray = new TypeInfo("decimal", true, true, false);
    public static readonly TypeInfo BoolNullableArray = new TypeInfo("bool", true, true, false);
    public static readonly TypeInfo DateTimeNullableArray = new TypeInfo("DateTime", true, true, false);
    public static readonly TypeInfo DateTimeOffsetNullableArray = new TypeInfo("DateTimeOffset", true, true, false);
    public static readonly TypeInfo TimeSpanNullableArray = new TypeInfo("TimeSpan", true, true, false);
    public static readonly TypeInfo GuidNullableArray = new TypeInfo("Guid", true, true, false);

    public static readonly TypeInfo SByteNullableList = new TypeInfo("sbyte", true, false, true);
    public static readonly TypeInfo ByteNullableList = new TypeInfo("byte", true, false, true);
    public static readonly TypeInfo ShortNullableList = new TypeInfo("short", true, false, true);
    public static readonly TypeInfo UShortNullableList = new TypeInfo("ushort", true, false, true);
    public static readonly TypeInfo IntNullableList = new TypeInfo("int", true, false, true);
    public static readonly TypeInfo UintNullableList = new TypeInfo("uint", true, false, true);
    public static readonly TypeInfo LongNullableList = new TypeInfo("long", true, false, true);
    public static readonly TypeInfo ULongNullableList = new TypeInfo("ulong", true, false, true);
    public static readonly TypeInfo FloatNullableList = new TypeInfo("float", true, false, true);
    public static readonly TypeInfo DoubleNullableList = new TypeInfo("double", true, false, true);
    public static readonly TypeInfo DecimalNullableList = new TypeInfo("decimal", true, false, true);
    public static readonly TypeInfo BoolNullableList = new TypeInfo("bool", true, false, true);
    public static readonly TypeInfo DateTimeNullableList = new TypeInfo("DateTime", true, false, true);
    public static readonly TypeInfo DateTimeOffsetNullableList = new TypeInfo("DateTimeOffset", true, false, true);
    public static readonly TypeInfo TimeSpanNullableList = new TypeInfo("TimeSpan", true, false, true);
    public static readonly TypeInfo GuidNullableList = new TypeInfo("Guid", true, false, true);

    private string _typeName = null;
    public override string ToString()
    {
        if(string.IsNullOrWhiteSpace(_typeName))
        {
            var nullable = IsNullable ? "?" : "";
            var array = IsArray ? "[]" : "";
            _typeName = $"{Name}{nullable}{array}";

            if(IsList)
                _typeName = $"List<{_typeName}>";
        }

        return _typeName;
    }
}

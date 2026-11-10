namespace SF.Db;

public static class SqlParams
{
    public static class Params
    {
        public static string ProcName { get; } = "SPECIFIC_NAME";
        public static string Name { get; } = "PARAMETER_NAME";
        public static string Ordinal { get; } = "ORDINAL_POSITION";
        public static string Mode { get; } = "PARAMETER_MODE";
        public static string IsResult { get; } = "IS_RESULT";
        public static string AsLocator { get; } = "AS_LOCATOR";
        public static string DataType { get; } = "DATA_TYPE";
        public static string MaxStringLength { get; } = "CHARACTER_MAXIMUM_LENGTH";
        public static string MaxBytesInString { get; } = "CHARACTER_OCTET_LENGTH";
        public static string NumberPrecision { get; } = "NUMERIC_PRECISION";
        public static string NumberPrecisionRadix { get; } = "NUMERIC_PRECISION_RADIX";
        public static string NumberScale { get; } = "NUMERIC_SCALE";
        public static string DateTimePrecision { get; } = "DATETIME_PRECISION";
    }
}

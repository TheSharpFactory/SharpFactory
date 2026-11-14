namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    #region Exception Helpers
    public static void ThrowNotImplementedException()
    {
        AppendIndent().AppendLine("throw new NotImplementedException(\"This feature has not been implemented.\");");
    }
    public static void ThrowNotSupportedException()
    {
        AppendIndent().AppendLine("throw new NotSupportedException(\"This feature is not supported.\");");
    }
    public static void ThrowArgumentException(string message)
    {
        AppendIndent().Append("throw new ArgumentException(").Append(message).AppendLine(");");
    }
    #endregion
}

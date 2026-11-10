namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    #region Exception Helpers
    public static void ThrowNotImplementedException()
    {
        _b.Append(_currentIndent).AppendLine("throw new NotImplementedException(\"This feature has not been implemented.\");");
    }
    public static void ThrowNotSupportedException()
    {
        _b.Append(_currentIndent).AppendLine("throw new NotSupportedException(\"This feature is not supported.\");");
    }
    public static void ThrowArgumentException(string message)
    {
        _b.Append(_currentIndent).Append("throw new ArgumentException(").Append(message).AppendLine(");");
    }
    #endregion
}

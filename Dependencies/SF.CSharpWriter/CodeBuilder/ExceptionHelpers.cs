namespace SF.CodeGeneration;

//throw statements for the common exceptions
public static partial class CSharpFile
{
    /// <summary>
    /// <c>throw new NotImplementedException("This feature has not been implemented.");</c>
    /// </summary>
    public static void ThrowNotImplementedException()
    {
        AppendIndent().AppendLine("throw new NotImplementedException(\"This feature has not been implemented.\");");
    }

    /// <summary>
    /// <c>throw new NotSupportedException("This feature is not supported.");</c>
    /// </summary>
    public static void ThrowNotSupportedException()
    {
        AppendIndent().AppendLine("throw new NotSupportedException(\"This feature is not supported.\");");
    }

    /// <summary>
    /// <c>throw new ArgumentException(message);</c>: <paramref name="message"/> is C#, so a literal comes quoted
    /// ("\"Bad value\"").
    /// </summary>
    public static void ThrowArgumentException(string message)
    {
        AppendIndent().Append("throw new ArgumentException(").Append(message).AppendLine(");");
    }
}

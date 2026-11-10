namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    /// <summary>
    /// A file-scoped namespace (C# 10; every supported target is .NET 10 or later).
    /// </summary>
    public static void OpenNamespace(string typeNamespace)
    {
        _b.Append("namespace ").Append(typeNamespace).Append(';').AppendLine();
    }

    /// <summary>
    /// Nothing to close: the namespace is file-scoped.
    /// </summary>
    public static void CloseNamespace()
    {
    }
}

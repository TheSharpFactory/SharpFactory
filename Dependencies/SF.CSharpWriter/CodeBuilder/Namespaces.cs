namespace SF.CodeGeneration;

//namespaces, both kinds: file-scoped (namespace name;, nothing to close) and with a body (namespace name { });
//CloseTypeWithNamespace works with either
public static partial class CSharpFile
{
    //how many namespace blocks are open: CloseTypeWithNamespace closes one only when there is one to close
    private static int _openNamespaceBlocks;

    /// <summary>
    /// A file-scoped namespace: <c>namespace name;</c>. Nothing closes it.
    /// </summary>
    public static void OpenNamespace(string name)
    {
        _b.Append("namespace ").Append(name).AppendLine(";");
    }

    /// <summary>
    /// A namespace with a body: <c>namespace name</c>, then '{', and what follows is indented. Close it with
    /// CloseNamespaceBlock (or CloseTypeWithNamespace after its last type). One file can hold several, and they can
    /// be nested.
    /// </summary>
    public static void OpenNamespaceBlock(string name)
    {
        AppendIndent().Append("namespace ").AppendLine(name);
        OpenBlock();
        _openNamespaceBlocks++;
    }

    /// <summary>
    /// Closes the innermost namespace opened with OpenNamespaceBlock.
    /// </summary>
    public static void CloseNamespaceBlock()
    {
        CloseBlock();
        if(_openNamespaceBlocks > 0)
            _openNamespaceBlocks--;
    }

    /// <summary>
    /// Closes the last type of a namespace: the type, then the innermost namespace block when one is open. With a
    /// file-scoped namespace (or none) it closes only the type.
    /// </summary>
    public static void CloseTypeWithNamespace()
    {
        CloseBlock();
        if(_openNamespaceBlocks > 0)
            CloseNamespaceBlock();
    }
}

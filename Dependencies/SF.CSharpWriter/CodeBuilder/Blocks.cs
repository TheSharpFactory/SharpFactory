namespace SF.CodeGeneration;

//braces: a block indents what follows until it is closed
public static partial class CSharpFile
{
    /// <summary>
    /// '{' at the current indentation; what follows is one level deeper until CloseBlock.
    /// </summary>
    public static void OpenBlock()
    {
        AppendIndent().AppendLine("{");
        Indent();
    }

    /// <summary>
    /// Goes back one level and writes '}'.
    /// </summary>
    public static void CloseBlock()
    {
        CloseBlock("}");
    }

    /// <summary>
    /// Closes a block passed as an argument: <c>});</c>
    /// </summary>
    public static void CloseBlockWithParenthesis()
    {
        CloseBlock("});");
    }

    /// <summary>
    /// Closes an initializer or a switch expression ended by a statement: <c>};</c>
    /// </summary>
    public static void CloseBlockWithSemicolon()
    {
        CloseBlock("};");
    }

    /// <summary>
    /// Closes an element of a list: <c>},</c>
    /// </summary>
    public static void CloseBlockWithComma()
    {
        CloseBlock("},");
    }

    /// <summary>
    /// Goes back one level and writes <paramref name="ending"/> in place of '}'.
    /// </summary>
    public static void CloseBlock(string ending)
    {
        Outdent();
        AppendIndent().AppendLine(ending);
    }
}

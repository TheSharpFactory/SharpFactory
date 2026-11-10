namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void OpenBlock()
    {
        _b.Append(_currentIndent).AppendLine("{");
        Indent();
    }
    public static void CloseBlock()
    {
        CloseBlock("}");
    }
    public static void CloseBlockWithParenthesis()
    {
        CloseBlock("});");
    }
    public static void CloseBlockWithSemicolon()
    {
        CloseBlock("};");
    }
    public static void CloseBlockWithComma()
    {
        CloseBlock("},");
    }
    public static void CloseBlock(string customEnding)
    {
        Outdent();
        _b.Append(_currentIndent).AppendLine(customEnding);
    }
}

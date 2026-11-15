namespace SF.CodeGeneration;

//if, while, do and switch: each writes only its own line; open the block next (or write one statement with
//LineIndented)
public static partial class CSharpFile
{
    #region If and Else
    /// <summary>
    /// <c>if(condition)</c>
    /// </summary>
    public static void If(string condition)
    {
        AppendIndent().Append("if(").Append(condition).AppendLine(")");
    }

    /// <summary>
    /// <c>else if(condition)</c>
    /// </summary>
    public static void ElseIf(string condition)
    {
        AppendIndent().Append("else if(").Append(condition).AppendLine(")");
    }

    /// <summary>
    /// <c>else</c>
    /// </summary>
    public static void Else()
    {
        AppendIndent().AppendLine("else");
    }
    #endregion

    #region While and Do
    /// <summary>
    /// <c>while(condition)</c>
    /// </summary>
    public static void While(string condition)
    {
        AppendIndent().Append("while(").Append(condition).AppendLine(")");
    }

    /// <summary>
    /// Opens a do/while loop: write the body as a block, then close the loop with DoWhile.
    /// </summary>
    public static void Do()
    {
        AppendIndent().AppendLine("do");
    }

    /// <summary>
    /// Closes a do/while loop with its condition: <c>while(condition);</c>
    /// </summary>
    public static void DoWhile(string condition)
    {
        AppendIndent().Append("while(").Append(condition).AppendLine(");");
    }
    #endregion

    #region Switch
    /// <summary>
    /// <c>switch(expression)</c>
    /// </summary>
    public static void Switch(string expression)
    {
        AppendIndent().Append("switch(").Append(expression).AppendLine(")");
    }

    /// <summary>
    /// <c>case value:</c> (a constant or a pattern).
    /// </summary>
    public static void Case(string value)
    {
        AppendIndent().Append("case ").Append(value).AppendLine(":");
    }

    /// <summary>
    /// <c>default:</c>
    /// </summary>
    public static void CaseDefault()
    {
        AppendIndent().AppendLine("default:");
    }

    /// <summary>
    /// <c>break;</c>
    /// </summary>
    public static void Break()
    {
        AppendIndent().AppendLine("break;");
    }
    #endregion
}

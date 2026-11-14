namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void If(string condition)
    {
        AppendIndent().Append("if(").Append(condition).AppendLine(")");
    }
    public static void ElseIf(string condition)
    {
        AppendIndent().Append("else if(").Append(condition).AppendLine(")");
    }
    public static void Else()
    {
        AppendIndent().AppendLine("else");
    }
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
    /// Closes a do/while loop with its condition.
    /// </summary>
    public static void DoWhile(string condition)
    {
        AppendIndent().Append("while(").Append(condition).AppendLine(");");
    }
    public static void Switch(string field)
    {
        AppendIndent().Append("switch(").Append(field).AppendLine(")");
    }
    public static void Case(string value)
    {
        AppendIndent().Append("case ").Append(value).AppendLine(":");
    }
    public static void CaseDefault()
    {
        AppendIndent().AppendLine("default:");
    }
    public static void Break()
    {
        AppendIndent().AppendLine("break;");
    }
    public static void ForEach(string variable, string collection)
    {
        AppendIndent()
          .Append("foreach(var ")
          .Append(variable)
          .Append(" in ")
          .Append(collection)
          .AppendLine(")");
    }
}

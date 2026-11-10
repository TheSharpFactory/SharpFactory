namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void If(string condition)
    {
        _b.Append(_currentIndent).Append("if(").Append(condition).AppendLine(")");
    }
    public static void ElseIf(string condition)
    {
        _b.Append(_currentIndent).Append("else if(").Append(condition).AppendLine(")");
    }
    public static void Else()
    {
        _b.Append(_currentIndent).AppendLine("else");
    }
    public static void While(string condition)
    {
        _b.Append(_currentIndent).Append("while(").Append(condition).AppendLine(")");
    }
    public static void Switch(string field)
    {
        _b.Append(_currentIndent).Append("switch(").Append(field).AppendLine(")");
    }
    public static void Case(string value)
    {
        _b.Append(_currentIndent).Append("case ").Append(value).AppendLine(":");
    }
    public static void CaseDefault()
    {
        _b.Append(_currentIndent).AppendLine("default:");
    }
    public static void Break()
    {
        _b.Append(_currentIndent).AppendLine("break;");
    }
    public static void ForEach(string variable, string collection)
    {
        _b.Append(_currentIndent)
          .Append("foreach(var ")
          .Append(variable)
          .Append(" in ")
          .Append(collection)
          .AppendLine(")");
    }
}

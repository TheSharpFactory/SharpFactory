namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void EnumMember(EnumMemberDeclaration member)
    {
        XmlSummary(member.Summary);
        _b.Append(_currentIndent)
          .Append(member.Name);

        if(!string.IsNullOrWhiteSpace(member.Value))
            _b.Append(" = ").Append(member.Value);

        _b.Append(',').AppendLine();
    }
}

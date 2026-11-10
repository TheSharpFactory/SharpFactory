namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void Field(FieldDeclaration field)
    {
        FieldXmlComment(field);

        MemberSignature(field, field.IsReadonly);

        if(!string.IsNullOrWhiteSpace(field.DefaultValue))
            _b.Append(" = ").Append(field.DefaultValue);

        _b.AppendLine(";");
    }
}

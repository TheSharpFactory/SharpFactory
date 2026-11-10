namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void OpenEnum(EnumDeclaration declaration)
    {
        XmlSummary(declaration.Summary);
        ComposeEnumDeclaration(declaration);
        OpenBlock();
    }
    public static void OpenEnumWithNamespace(EnumDeclaration declaration)
    {
        OpenNamespace(declaration.Namespace);
        OpenEnum(declaration);
    }
    private static void ComposeEnumDeclaration(EnumDeclaration declaration)
    {
        _b.Append(_currentIndent);

        _b.Append(_accessModifiers[declaration.AccessModifier]);

        _b.Append(" enum ");

        _b.Append(declaration.Name);

        if(declaration.BaseType != null)
            _b.Append(" : ").Append(declaration.BaseType);

        _b.AppendLine();
    }

    public static void CloseEnum()
    {
        CloseBlock();
    }
    public static void CloseEnumWithNamespace()
    {
        CloseEnum();
        CloseNamespace();
    }
}

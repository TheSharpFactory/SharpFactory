namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void OpenInterface(InterfaceDeclaration declaration)
    {
        XmlSummary(declaration.Summary);
        ComposeInterfaceDeclaration(declaration);
        OpenBlock();
    }
    public static void OpenInterfaceWithNamespace(InterfaceDeclaration declaration)
    {
        OpenNamespace(declaration.Namespace);
        OpenInterface(declaration);
    }
    private static void ComposeInterfaceDeclaration(InterfaceDeclaration declaration)
    {
        _b.Append(_currentIndent);

        _b.Append(_accessModifiers[declaration.AccessModifier]);

        if(declaration.IsPartial)
            _b.Append(' ').Append("partial");

        _b.Append(' ').Append("interface");

        _b.Append(' ').Append(declaration.Name);

        ComposeGenericParams(declaration);

        ComposeInterfaceInheritance(declaration);

        ComposeGenericConstraints(declaration);

        _b.AppendLine();
    }
    private static void ComposeInterfaceInheritance(IPolymorphicDeclaration type)
    {
        if(!type.ImplementsInterfaces)
            return;

        _b.Append(" : ").Append(string.Join(", ", type.Interfaces));
    }
    public static void CloseInterface()
    {
        CloseBlock();
    }
    public static void CloseInterfaceWithNamespace()
    {
        CloseInterface();
        CloseNamespace();
    }
}

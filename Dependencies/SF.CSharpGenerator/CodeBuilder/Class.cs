namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void OpenClass(ClassDeclaration declaration)
    {
        XmlSummary(declaration.Summary);
        ComposeClassAttributes(declaration);
        ComposeClassDeclaration(declaration);
        OpenBlock();
    }
    public static void OpenClassWithNamespace(ClassDeclaration declaration)
    {
        OpenNamespace(declaration.Namespace);
        OpenClass(declaration);
    }
    private static void ComposeClassDeclaration(ClassDeclaration declaration)
    {
        _b.Append(_currentIndent);

        _b.Append(_accessModifiers[declaration.AccessModifier]);

        if(declaration.IsStatic)
            _b.Append(' ').Append("static");

        if(declaration.InheritanceModifier != InheritanceModifiers.None)
            _b.Append(' ').Append(_inheritanceModifiers[declaration.InheritanceModifier]);

        if(declaration.IsPartial)
            _b.Append(' ').Append("partial");

        _b.Append(' ').Append("class");

        _b.Append(' ').Append(declaration.Name);

        ComposeGenericParams(declaration);

        ComposeClassInheritance(declaration);

        ComposeGenericConstraints(declaration);

        _b.AppendLine();
    }

    private static void ComposeClassAttributes(ClassDeclaration declaration)
    {
        var attributes = declaration.Attributes;

        if(!(attributes?.Count > 0))
            return;

        var attrCount = attributes.Count;

        for(var i = 0; i < attrCount; i++)
        {
            _b.Append(_currentIndent);

            _b.Append('[');

            _b.Append(attributes[i]);

            _b.Append(']');

            _b.AppendLine();
        }
    }

    private static void ComposeClassInheritance(ClassDeclaration type)
    {
        if(!type.ImplementsInterfaces && !type.IsDerived)
            return;

        _b.Append(" : ");

        if(type.IsDerived)
            _b.Append(type.BaseClass).Append(',');

        if(type.ImplementsInterfaces)
        {
            foreach(var i in type.Interfaces)
                _b.Append(i).Append(',');
        }

        _b.Length--;//remove last unused comma
    }

    public static void CloseClass()
    {
        CloseBlock();
    }
    public static void CloseClassWithNamespace()
    {
        CloseClass();
        CloseNamespace();
    }
}

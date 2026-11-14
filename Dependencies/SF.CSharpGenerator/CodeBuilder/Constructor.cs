namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void Constructor(ConstructorDeclaration constructor)
    {
        ConstructorXmlComment(constructor);

        ComposeConstructorSignature(constructor);

        _b.Append('(');

        if(constructor.Parameters?.Count > 0)
        {
            var count = constructor.Parameters.Count;
            for(var i = 0; i < count; i++)
                _b.Append(constructor.Parameters[i].ParamType).Append(' ').Append(constructor.Parameters[i].Name).Append(',').Append(' ');

            _b.Length--;
            _b.Length--;
        }

        _b.Append(')');

        if(!string.IsNullOrWhiteSpace(constructor.BaseConstructor))
            _b.Append(" : ").Append(constructor.BaseConstructor);

        _b.AppendLine();
    }

    public static void ConstructorEmpty(ConstructorDeclaration member)
    {
        Constructor(member);
        OpenBlock();
        CloseBlock();
    }

    /// <summary>
    /// "public Name" or, for a static constructor (which takes no access modifier), "static Name".
    /// </summary>
    private static void ComposeConstructorSignature(ConstructorDeclaration member)
    {
        AppendIndent();

        if(member.IsStatic)
            _b.Append("static");
        else
            _b.Append(_accessModifiers[member.AccessModifier]);

        _b.Append(' ').Append(member.Name);
    }
}

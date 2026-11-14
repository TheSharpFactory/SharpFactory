#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void Header(IDeclaration type, string token)
    {
        var typeKind = type.Artifact.ToString();
        _b.AppendLine(token);
        _b.AppendLine("/************************************************");
        _b.Append("This ").Append(typeKind).AppendLine(" has been generated dynamically.");
        _b.AppendLine("Any changes you make here will be overwritten.");

        if(type.IsPartial)
        {
            _b.Append("To add your own code create a partial ").Append(typeKind).AppendLine(" in");
            _b.AppendLine("a separate file with the following declaration:");
            TypeDeclarationExample(type);
        }

        _b.AppendLine("************************************************/");
        _b.AppendLine();
    }
    public static void Header(ArtifactKind type, string token)
    {
        var typeKind = type.ToString();
        _b.AppendLine(token);
        _b.AppendLine("/************************************************");
        _b.Append("This ").Append(typeKind).AppendLine(" has been generated dynamically.");
        _b.AppendLine("Any changes you make here will be overwritten.");
        _b.AppendLine("************************************************/");
        _b.AppendLine();
    }
    private static void TypeDeclarationExample(IDeclaration type)
    {
        _b.AppendLine();
        OpenNamespace(type.Namespace);

        AppendIndent();

        _b.Append(_accessModifiers[type.AccessModifier]);

        if(type.IsPartial)
            _b.Append(" partial");

        _b.Append(' ').Append(DescribeKeyword(type.Artifact)).Append(' ');

        _b.Append(type.Name);
        //a generic type's other part needs its type parameters, or it declares another type
        if(type is IGenericDeclaration genericType)
            ComposeGenericParams(genericType);

        _b.AppendLine();

        OpenBlock();
        CloseBlock();

        CloseNamespace();
        _b.AppendLine();
    }
    private static string DescribeKeyword(ArtifactKind kind)
    {
        switch(kind)
        {
            case ArtifactKind.Class:
                return "class";
            case ArtifactKind.Interface:
                return "interface";
            case ArtifactKind.Enum:
                return "enum";
            case ArtifactKind.Struct:
                return "struct";
            default:
                return kind.ToString().ToLowerInvariant();
        }
    }
    public static void Footer(List<string> footer = null)
    {
        if(!(footer?.Count > 0))
            return;

        _b.AppendLine("/************************************************");

        foreach(var l in footer)
            _b.AppendLine(l);

        _b.AppendLine("************************************************/");
    }
}

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
        _b.AppendLine($"This {typeKind} has been generated dynamically.");
        _b.AppendLine("Any changes you make here will be overwritten.");

        if(type.IsPartial)
        {
            _b.AppendLine($"To add your own code create a partial {typeKind} in");
            _b.AppendLine("a sepparate file with the following declaration:");
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
        _b.AppendLine($"This {typeKind} has been generated dynamically.");
        _b.AppendLine("Any changes you make here will be overwritten.");
        _b.AppendLine("************************************************/");
        _b.AppendLine();
    }
    private static void TypeDeclarationExample(IDeclaration type)
    {
        _b.AppendLine();
        OpenNamespace(type.Namespace);

        _b.Append(_currentIndent);

        _b.Append(_accessModifiers[type.AccessModifier]);

        if(type.IsPartial)
            _b.Append(" partial");

        _b.Append(' ').Append(type.Artifact.ToString().ToLower()).Append(' ');

        _b.AppendLine(type.Name);

        OpenBlock();
        CloseBlock();

        CloseNamespace();
        _b.AppendLine();
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

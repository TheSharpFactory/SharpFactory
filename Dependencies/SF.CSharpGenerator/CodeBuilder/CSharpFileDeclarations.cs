#region Usings
using System;
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

/// <summary>
/// SharpFactory's declarations (classes, interfaces, enums, members, doc comments, the file header) written with the
/// CSharpFile library. The methods are static extension members of CSharpFile, so a generator still calls
/// CSharpFile.DeclareMethod(method), CSharpFile.OpenClass(declaration) and so on. Everything goes through the
/// library's public methods; this file holds the shared helpers.
/// </summary>
public static partial class CSharpFileDeclarations
{
    private static readonly Dictionary<AccessModifiers, string> _accessModifiers = new Dictionary<AccessModifiers, string>(7)
    {
        { AccessModifiers.Public, AccessModifierConstants.Public },
        { AccessModifiers.Private, AccessModifierConstants.Private },
        { AccessModifiers.Protected, AccessModifierConstants.Protected },
        { AccessModifiers.Internal, AccessModifierConstants.Internal },
        { AccessModifiers.ProtectedInternal, AccessModifierConstants.ProtectedInternal },
        { AccessModifiers.PrivateProtected, AccessModifierConstants.PrivateProtected },
        { AccessModifiers.None, AccessModifierConstants.None },
    };

    private static readonly Dictionary<InheritanceModifiers, string> _inheritanceModifiers = new Dictionary<InheritanceModifiers, string>(7)
    {
        { InheritanceModifiers.Abstract, InheritanceModifierConstants.Abstract },
        { InheritanceModifiers.Sealed, InheritanceModifierConstants.Sealed },
        { InheritanceModifiers.Override, InheritanceModifierConstants.Override },
        { InheritanceModifiers.Virtual, InheritanceModifierConstants.Virtual },
        { InheritanceModifiers.New, InheritanceModifierConstants.New },
        { InheritanceModifiers.None, InheritanceModifierConstants.None },
    };

    /// <summary>
    /// The indentation, the modifiers, the return type and the name of a member; the line is left open.
    /// </summary>
    private static void MemberSignature(IComplexDeclaration member, bool isReadOnly = false)
    {
        CSharpFile.AppendIndented("");

        var hideMember = member.InheritanceModifier == InheritanceModifiers.New;
        var isMethod = member.Artifact == ArtifactKind.Method;
        if(!member.IsInterfaceMember)
        {
            if(isMethod && member.IsPartial)
                CSharpFile.Append("partial ");
            else
            {
                //each modifier with the space after it; None (an explicit interface implementation) writes nothing
                if(member.AccessModifier != AccessModifiers.None)
                {
                    CSharpFile.Append(_accessModifiers[member.AccessModifier]);
                    CSharpFile.Append(' ');
                }

                if(member.IsStatic)
                    CSharpFile.Append("static ");

                if(hideMember)
                    CSharpFile.Append("new ");

                if(isReadOnly)
                    CSharpFile.Append("readonly ");

                if(!hideMember && member.InheritanceModifier != InheritanceModifiers.None)
                {
                    CSharpFile.Append(_inheritanceModifiers[member.InheritanceModifier]);
                    CSharpFile.Append(' ');
                }
            }
        }

        if(isMethod && ((IMethodDeclaration)member).IsAsync)
        {
            if(!member.IsInterfaceMember && !((IMethodDeclaration)member).SkipAwait)
                CSharpFile.Append("async ");

            if(!member.ReturnType.IsVoid && !member.ReturnType.IsTask)
            {
                CSharpFile.Append("Task<");
                AppendText(member.ReturnType);
                CSharpFile.Append('>');
            }
            else
                AppendText(member.ReturnType);
        }
        else
            AppendText(member.ReturnType);

        CSharpFile.Append(' ');
        CSharpFile.Append(member.Name);
    }

    private static void ComposeGenericParams(IGenericDeclaration type)
    {
        if(!type.IsGeneric)
            return;

        CSharpFile.Append('<');
        var separator = "";
        foreach(var g in type.GenericParams)
        {
            CSharpFile.Append(separator);
            CSharpFile.Append(g.Name);
            separator = ", ";
        }

        CSharpFile.Append('>');
    }

    private static void ComposeGenericConstraints(IGenericDeclaration type)
    {
        if(!type.IsGeneric)
            return;

        foreach(var g in type.GenericParams)
        {
            if(string.IsNullOrWhiteSpace(g.Constraint))
                continue;

            CSharpFile.Append(" where ");
            CSharpFile.Append(g.Name);
            CSharpFile.Append(" : ");
            CSharpFile.Append(g.Constraint);
        }
    }

    /// <summary>
    /// The parameters of a method or a constructor, without the parentheses: "type name = default, ...".
    /// </summary>
    private static void ComposeParameters(List<MethodParameter> parameters, bool withDefaults)
    {
        if(!(parameters?.Count > 0))
            return;

        var count = parameters.Count;
        for(var i = 0; i < count; i++)
        {
            var p = parameters[i];
            if(i > 0)
                CSharpFile.Append(", ");

            CSharpFile.Append(p.ParamType);
            CSharpFile.Append(' ');
            CSharpFile.Append(p.Name);
            if(withDefaults && !string.IsNullOrWhiteSpace(p.DefaultValue))
            {
                CSharpFile.Append(" = ");
                CSharpFile.Append(p.DefaultValue);
            }
        }
    }

    /// <summary>
    /// The text of an object (a TypeInfo writes its type name); null writes nothing.
    /// </summary>
    private static void AppendText(object value)
    {
        if(value != null)
            CSharpFile.Append(value.ToString());
    }

    /// <summary>
    /// Ends the line with the member's type in words: its ReturnTypeForComments, or its type with "&lt;" read as
    /// " of " and "&gt;" left out (List&lt;Region&gt; is "List of Region"). Written straight from the type name, in
    /// slices, without building the text first.
    /// </summary>
    private static void AppendTypeDescription(IComplexDeclaration member)
    {
        if(!string.IsNullOrWhiteSpace(member.ReturnTypeForComments))
        {
            CSharpFile.Append(member.ReturnTypeForComments);
            CSharpFile.EmptyLine();
            return;
        }

        var rest = member.ReturnType.ToString().AsSpan();
        var next = rest.IndexOfAny('<', '>');
        while(next >= 0)
        {
            CSharpFile.Append($"{rest.Slice(0, next)}");
            if(rest[next] == '<')
                CSharpFile.Append(" of ");

            rest = rest.Slice(next + 1);
            next = rest.IndexOfAny('<', '>');
        }

        CSharpFile.Append($"{rest}");
        CSharpFile.EmptyLine();
    }

    /// <summary>
    /// A doc comment's summary, one &lt;para&gt; per line, after an empty line.
    /// </summary>
    private static void XmlSummaryLines(List<string> summary)
    {
        if(!(summary?.Count > 0))
            return;

        CSharpFile.EmptyLine();
        CSharpFile.LineNoEnding("/// <summary>");
        foreach(var line in summary)
        {
            CSharpFile.AppendIndented("/// <para>");
            CSharpFile.Append(line);
            CSharpFile.Append("</para>");
            CSharpFile.EmptyLine();
        }

        CSharpFile.LineNoEnding("/// </summary>");
    }

    private static void XmlParams(List<MethodParameter> parameters)
    {
        if(!(parameters?.Count > 0))
            return;

        foreach(var p in parameters)
        {
            CSharpFile.AppendIndented("/// <param name=\"");
            CSharpFile.Append(p.Name);
            CSharpFile.Append("\">");
            CSharpFile.Append(p.Description);
            CSharpFile.Append("</param>");
            CSharpFile.EmptyLine();
        }
    }
}

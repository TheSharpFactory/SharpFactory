#region Usings
using System;
#endregion

namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void Property(PropertyDeclaration prop)
    {
        PropXmlComment(prop);

        MemberSignature(prop);

        _b.Append(' ').Append('{');

        switch(prop.Kind)
        {
            case PropertyKind.Auto:
                _b.Append(" get; ");

                if(prop.SetterModifier != AccessModifiers.None)
                    _b.Append(_accessModifiers[prop.SetterModifier]).Append(' ');

                _b.Append("set;");
                break;
            case PropertyKind.AutoNoSetter:
                _b.Append(" get;");
                break;
            case PropertyKind.Full:
            case PropertyKind.FullNoSetter:
            {
                ComposeBackingFieldName(prop);
                _b.Append(" get{ return ").Append(prop.BackingField).Append("; } ");

                if(prop.Kind == PropertyKind.FullNoSetter)
                    break;

                if(prop.SetterModifier != AccessModifiers.None)
                    _b.Append(_accessModifiers[prop.SetterModifier]).Append(' ');

                _b.Append("set{ ").Append(prop.BackingField).Append(" = value; } ");
            }

            break;
            default:
                throw new ArgumentException($"Property kind '{prop.Kind}' is not supported.");
        }

        _b.Append(' ').Append('}');

        if(!string.IsNullOrWhiteSpace(prop.DefaultValue))
            _b.Append(" = ").Append(prop.DefaultValue).Append(';');

        _b.AppendLine();
    }

    public static void OpenProperty(PropertyDeclaration prop)
    {
        PropXmlComment(prop);

        MemberSignature(prop);

        _b.AppendLine();

        OpenBlock();
    }
    public static void CloseProperty()
    {
        CloseBlock();
    }
    public static void OpenGetter()
    {
        _b.Append(_currentIndent).AppendLine("get");
        OpenBlock();
    }
    public static void CloseGetter()
    {
        CloseBlock();
    }
    public static void OpenSetter()
    {
        _b.Append(_currentIndent).AppendLine("set");
        OpenBlock();
    }
    public static void CloseSetter()
    {
        CloseBlock();
    }
    private static void ComposeBackingFieldName(PropertyDeclaration prop)
    {
        if(!string.IsNullOrWhiteSpace(prop.BackingField))
            return;

        prop.BackingField = "_" + prop.Name[0].ToString().ToLower() + (prop.Name.Length > 1 ? prop.Name.Substring(1) : "");
    }
}

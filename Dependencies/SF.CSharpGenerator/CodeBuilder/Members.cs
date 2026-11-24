#region Usings
using System;
#endregion

namespace SF.CodeGeneration;

//fields, properties, methods and constructors from their declarations
public static partial class CSharpFileDeclarations
{
    extension(CSharpFile)
    {
        #region Fields
        public static void Field(FieldDeclaration field)
        {
            CSharpFile.FieldXmlComment(field);
            MemberSignature(field, field.IsReadonly);
            if(!string.IsNullOrWhiteSpace(field.DefaultValue))
            {
                CSharpFile.Append(" = ");
                CSharpFile.Append(field.DefaultValue);
            }

            CSharpFile.Append(';');
            CSharpFile.EmptyLine();
        }
        #endregion

        #region Properties
        public static void Property(PropertyDeclaration prop)
        {
            CSharpFile.PropXmlComment(prop);
            if(prop.Attributes?.Count > 0)
            {
                foreach(var attribute in prop.Attributes)
                    CSharpFile.Attribute(attribute);
            }

            MemberSignature(prop);
            CSharpFile.Append(" {");
            switch(prop.Kind)
            {
                case PropertyKind.Auto:
                    CSharpFile.Append(" get; ");
                    AppendSetterModifier(prop);
                    CSharpFile.Append("set;");
                    break;
                case PropertyKind.AutoNoSetter:
                    CSharpFile.Append(" get;");
                    break;
                case PropertyKind.Full:
                case PropertyKind.FullNoSetter:
                    ComposeBackingFieldName(prop);
                    CSharpFile.Append(" get{ return ");
                    CSharpFile.Append(prop.BackingField);
                    CSharpFile.Append("; }");
                    if(prop.Kind == PropertyKind.FullNoSetter)
                        break;

                    CSharpFile.Append(' ');
                    AppendSetterModifier(prop);
                    CSharpFile.Append("set{ ");
                    CSharpFile.Append(prop.BackingField);
                    CSharpFile.Append(" = value; }");
                    break;
                default:
                    throw new ArgumentException($"Property kind '{prop.Kind}' is not supported.");
            }

            CSharpFile.Append(" }");
            if(!string.IsNullOrWhiteSpace(prop.DefaultValue))
            {
                CSharpFile.Append(" = ");
                CSharpFile.Append(prop.DefaultValue);
                CSharpFile.Append(';');
            }

            CSharpFile.EmptyLine();
        }

        public static void OpenProperty(PropertyDeclaration prop)
        {
            CSharpFile.PropXmlComment(prop);
            MemberSignature(prop);
            CSharpFile.EmptyLine();
            CSharpFile.OpenBlock();
        }

        public static void CloseProperty()
        {
            CSharpFile.CloseBlock();
        }

        public static void OpenGetter()
        {
            CSharpFile.LineNoEnding("get");
            CSharpFile.OpenBlock();
        }

        public static void CloseGetter()
        {
            CSharpFile.CloseBlock();
        }

        public static void OpenSetter()
        {
            CSharpFile.LineNoEnding("set");
            CSharpFile.OpenBlock();
        }

        public static void CloseSetter()
        {
            CSharpFile.CloseBlock();
        }
        #endregion

        #region Methods
        public static void DeclareMethod(MethodDeclaration method)
        {
            CSharpFile.MethodXmlComment(method);
            if(method.Attributes?.Count > 0)
            {
                foreach(var attribute in method.Attributes)
                    CSharpFile.Attribute(attribute);
            }

            MemberSignature(method);
            ComposeGenericParams(method);
            CSharpFile.Append('(');
            ComposeParameters(method.Parameters, true);
            CSharpFile.Append(')');
            ComposeGenericConstraints(method);
            if(method.IsInterfaceMember || method.InheritanceModifier == InheritanceModifiers.Abstract || method.IsPartial)
                CSharpFile.Append(';');

            CSharpFile.EmptyLine();
        }
        #endregion

        #region Constructors
        public static void Constructor(ConstructorDeclaration constructor)
        {
            CSharpFile.ConstructorXmlComment(constructor);

            //a static constructor takes no access modifier
            CSharpFile.AppendIndented(constructor.IsStatic ? "static" : _accessModifiers[constructor.AccessModifier]);
            CSharpFile.Append(' ');
            CSharpFile.Append(constructor.Name);
            CSharpFile.Append('(');
            ComposeParameters(constructor.Parameters, false);
            CSharpFile.Append(')');
            if(!string.IsNullOrWhiteSpace(constructor.BaseConstructor))
            {
                CSharpFile.Append(" : ");
                CSharpFile.Append(constructor.BaseConstructor);
            }

            CSharpFile.EmptyLine();
        }

        public static void ConstructorEmpty(ConstructorDeclaration member)
        {
            CSharpFile.Constructor(member);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();
        }
        #endregion
    }

    private static void AppendSetterModifier(PropertyDeclaration prop)
    {
        if(prop.SetterModifier == AccessModifiers.None)
            return;

        CSharpFile.Append(_accessModifiers[prop.SetterModifier]);
        CSharpFile.Append(' ');
    }

    private static void ComposeBackingFieldName(PropertyDeclaration prop)
    {
        if(!string.IsNullOrWhiteSpace(prop.BackingField))
            return;

        prop.BackingField = "_" + prop.Name[0].ToString().ToLowerInvariant() + (prop.Name.Length > 1 ? prop.Name.Substring(1) : "");
    }
}

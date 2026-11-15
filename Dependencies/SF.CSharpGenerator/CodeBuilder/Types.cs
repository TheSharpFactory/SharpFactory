namespace SF.CodeGeneration;

//classes, interfaces and enums from their declarations
public static partial class CSharpFileDeclarations
{
    extension(CSharpFile)
    {
        #region Classes
        public static void OpenClass(ClassDeclaration declaration)
        {
            XmlSummaryLines(declaration.Summary);
            if(declaration.Attributes?.Count > 0)
            {
                foreach(var attribute in declaration.Attributes)
                    CSharpFile.Attribute(attribute);
            }

            CSharpFile.AppendIndented(_accessModifiers[declaration.AccessModifier]);
            if(declaration.IsStatic)
                CSharpFile.Append(" static");

            if(declaration.InheritanceModifier != InheritanceModifiers.None)
            {
                CSharpFile.Append(' ');
                CSharpFile.Append(_inheritanceModifiers[declaration.InheritanceModifier]);
            }

            if(declaration.IsPartial)
                CSharpFile.Append(" partial");

            CSharpFile.Append(" class ");
            CSharpFile.Append(declaration.Name);
            ComposeGenericParams(declaration);
            if(declaration.IsDerived || declaration.ImplementsInterfaces)
            {
                CSharpFile.Append(" : ");
                var separator = "";
                if(declaration.IsDerived)
                {
                    CSharpFile.Append(declaration.BaseClass);
                    separator = ", ";
                }

                if(declaration.ImplementsInterfaces)
                {
                    foreach(var i in declaration.Interfaces)
                    {
                        CSharpFile.Append(separator);
                        CSharpFile.Append(i);
                        separator = ", ";
                    }
                }
            }

            ComposeGenericConstraints(declaration);
            CSharpFile.EmptyLine();
            CSharpFile.OpenBlock();
        }

        public static void OpenClassWithNamespace(ClassDeclaration declaration)
        {
            CSharpFile.OpenNamespace(declaration.Namespace);
            CSharpFile.OpenClass(declaration);
        }

        public static void CloseClass()
        {
            CSharpFile.CloseBlock();
        }

        public static void CloseClassWithNamespace()
        {
            CSharpFile.CloseClass();
            CSharpFile.CloseNamespace();
        }
        #endregion

        #region Interfaces
        public static void OpenInterface(InterfaceDeclaration declaration)
        {
            XmlSummaryLines(declaration.Summary);
            CSharpFile.AppendIndented(_accessModifiers[declaration.AccessModifier]);
            if(declaration.IsPartial)
                CSharpFile.Append(" partial");

            CSharpFile.Append(" interface ");
            CSharpFile.Append(declaration.Name);
            ComposeGenericParams(declaration);
            if(declaration.ImplementsInterfaces)
            {
                CSharpFile.Append(" : ");
                var count = declaration.Interfaces.Count;
                for(var i = 0; i < count; i++)
                {
                    if(i > 0)
                        CSharpFile.Append(", ");

                    CSharpFile.Append(declaration.Interfaces[i]);
                }
            }

            ComposeGenericConstraints(declaration);
            CSharpFile.EmptyLine();
            CSharpFile.OpenBlock();
        }

        public static void OpenInterfaceWithNamespace(InterfaceDeclaration declaration)
        {
            CSharpFile.OpenNamespace(declaration.Namespace);
            CSharpFile.OpenInterface(declaration);
        }

        public static void CloseInterface()
        {
            CSharpFile.CloseBlock();
        }

        public static void CloseInterfaceWithNamespace()
        {
            CSharpFile.CloseInterface();
            CSharpFile.CloseNamespace();
        }
        #endregion

        #region Enums
        public static void OpenEnum(EnumDeclaration declaration)
        {
            XmlSummaryLines(declaration.Summary);
            CSharpFile.OpenEnum(_accessModifiers[declaration.AccessModifier], declaration.Name, declaration.BaseType?.ToString());
        }

        public static void OpenEnumWithNamespace(EnumDeclaration declaration)
        {
            CSharpFile.OpenNamespace(declaration.Namespace);
            CSharpFile.OpenEnum(declaration);
        }

        public static void CloseEnum()
        {
            CSharpFile.CloseBlock();
        }

        public static void CloseEnumWithNamespace()
        {
            CSharpFile.CloseEnum();
            CSharpFile.CloseNamespace();
        }

        public static void EnumMember(EnumMemberDeclaration member)
        {
            XmlSummaryLines(member.Summary);
            if(string.IsNullOrWhiteSpace(member.Value))
                CSharpFile.EnumMember(member.Name);
            else
                CSharpFile.EnumMember(member.Name, member.Value);
        }
        #endregion

        #region Namespaces
        /// <summary>
        /// Nothing to close: the namespace is file-scoped.
        /// </summary>
        public static void CloseNamespace()
        {
        }
        #endregion
    }
}

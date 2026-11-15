#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

//the doc comments of the declarations, the generated-file header and the usings
public static partial class CSharpFileDeclarations
{
    extension(CSharpFile)
    {
        #region Doc Comments
        public static void PropXmlComment(PropertyDeclaration prop)
        {
            if(!(prop.Summary?.Count > 0))
                return;

            XmlSummaryLines(prop.Summary);

            //a property documents its value; <returns> is for methods
            CSharpFile.LineNoEnding("/// <value>");
            CSharpFile.AppendIndented("/// ");
            AppendTypeDescription(prop);
            CSharpFile.LineNoEnding("/// </value>");
        }

        public static void FieldXmlComment(FieldDeclaration field)
        {
            if(!(field.Summary?.Count > 0))
                return;

            XmlSummaryLines(field.Summary);
        }

        public static void MethodXmlComment(MethodDeclaration method)
        {
            if(!(method.Summary?.Count > 0))
                return;

            XmlSummaryLines(method.Summary);
            XmlParams(method.Parameters);

            //methods without parameters also get the <returns> comment
            CSharpFile.LineNoEnding("/// <returns>");
            CSharpFile.AppendIndented("/// ");
            AppendTypeDescription(method);
            CSharpFile.LineNoEnding("/// </returns>");
        }

        public static void ConstructorXmlComment(ConstructorDeclaration method)
        {
            if(!(method.Summary?.Count > 0))
                return;

            XmlSummaryLines(method.Summary);
            XmlParams(method.Parameters);
        }
        #endregion

        #region Header
        public static void Header(IDeclaration type, string token)
        {
            var typeKind = type.Artifact.ToString();
            HeaderStart(typeKind, token);
            if(type.IsPartial)
            {
                CSharpFile.Append("To add your own code create a partial ");
                CSharpFile.Append(typeKind);
                CSharpFile.Append(" in");
                CSharpFile.EmptyLine();
                CSharpFile.Append("a separate file with the following declaration:");
                CSharpFile.EmptyLine();
                TypeDeclarationExample(type);
            }

            HeaderEnd();
        }

        public static void Header(ArtifactKind type, string token)
        {
            HeaderStart(type.ToString(), token);
            HeaderEnd();
        }
        #endregion

        #region Usings
        public static void UsingStatements(List<UsingNamespace> usings)
        {
            if(usings == null || usings.Count < 1)
                return;

            var alreadyAdded = new HashSet<string>(usings.Count);
            foreach(var ns in usings)
            {
                var usingId = $"{ns.Name}-{ns.Alias}";
                if(alreadyAdded.Contains(usingId))
                    continue;

                alreadyAdded.Add(usingId);
                if(!ns.AliasRequired)
                    CSharpFile.UsingNamespace(ns.Name);
                else
                {
                    //the alias comment goes on the same line, right after the ';'
                    CSharpFile.Append("using ");
                    CSharpFile.Append(ns.Alias);
                    CSharpFile.Append(" = ");
                    CSharpFile.Append(ns.Name);
                    CSharpFile.Append(';');
                    CSharpFile.Append(ns.AliasComment);
                    CSharpFile.EmptyLine();
                }
            }

            CSharpFile.EmptyLine();
        }
        #endregion
    }

    private static void HeaderStart(string typeKind, string token)
    {
        CSharpFile.Append(token);
        CSharpFile.EmptyLine();
        CSharpFile.Append("/************************************************");
        CSharpFile.EmptyLine();
        CSharpFile.Append("This ");
        CSharpFile.Append(typeKind);
        CSharpFile.Append(" has been generated dynamically.");
        CSharpFile.EmptyLine();
        CSharpFile.Append("Any changes you make here will be overwritten.");
        CSharpFile.EmptyLine();
    }

    private static void HeaderEnd()
    {
        CSharpFile.Append("************************************************/");
        CSharpFile.EmptyLine();
        CSharpFile.EmptyLine();
    }

    private static void TypeDeclarationExample(IDeclaration type)
    {
        CSharpFile.EmptyLine();
        CSharpFile.OpenNamespace(type.Namespace);
        CSharpFile.AppendIndented(_accessModifiers[type.AccessModifier]);
        if(type.IsPartial)
            CSharpFile.Append(" partial");

        CSharpFile.Append(' ');
        CSharpFile.Append(DescribeKeyword(type.Artifact));
        CSharpFile.Append(' ');
        CSharpFile.Append(type.Name);

        //a generic type's other part needs its type parameters, or it declares another type
        if(type is IGenericDeclaration genericType)
            ComposeGenericParams(genericType);

        CSharpFile.EmptyLine();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlock();
        CSharpFile.CloseNamespace();
        CSharpFile.EmptyLine();
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
}

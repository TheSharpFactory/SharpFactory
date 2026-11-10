#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateComparers(ModelContainer modelContainer)
    {
        return Comparers.Generate(modelContainer);
    }
    private static class Comparers
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            //the first one does not have nav props for entities that do not have any
            //var typeDeclaration = $"public partial class {Common.Names.Compare()}";

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(1)
            {
              new UsingNamespace { Name = "System" },
            };

            var summary = "This class holds methods to compare types.";

            var type = new ClassDeclaration
            {
                Name = Common.Names.Compare,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string> { summary }
            };
            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            //open namespace
            CSharpFile.OpenClassWithNamespace(type);
            //generate body
            GenerateBody();
            //close namespace
            CSharpFile.CloseClassWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
        private static void GenerateBody()
        {
            GenerateCompareArrayFunction();
        }
        private static void GenerateCompareArrayFunction()
        {
            EntityUtilsMethodSignatures.GenerateCompareByteArrays();

            CSharpFile.OpenBlock();
            CSharpFile.If("a == null && b == null");
            CSharpFile.ReturnIndented("true");
            CSharpFile.If("a == null && b != null");
            CSharpFile.ReturnIndented("false");
            CSharpFile.If("a != null && b == null");
            CSharpFile.ReturnIndented("false");
            CSharpFile.If("a.Length != b.Length");
            CSharpFile.ReturnIndented("false");
            CSharpFile.If("object.ReferenceEquals(a, b)");
            CSharpFile.ReturnIndented("true");
            CSharpFile.Line("var length = a.Length");
            CSharpFile.LineNoEnding("for(int i = 0; i < length; i++)");
            CSharpFile.Indent();
            CSharpFile.If("a[i] != b[i]");
            CSharpFile.ReturnIndented("false");
            CSharpFile.Outdent();
            CSharpFile.Return("true");
            CSharpFile.CloseBlock();
        }
    }
}

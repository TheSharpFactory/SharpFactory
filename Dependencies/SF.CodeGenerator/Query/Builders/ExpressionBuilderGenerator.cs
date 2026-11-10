#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateExpressionBuilder(ModelContainer modelContainer)
    {
        return ExpressionBuilder.Generate(modelContainer);
    }
    private static class ExpressionBuilder
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeName = Common.Names.ExpressionBuilder;

            //var typeDeclaration = $"public sealed class {typeName}";

            //var summary = "Builds a group of conditions(an expression) within the predicate(where clause) of a query.";

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(1)
    {
      new UsingNamespace { Name = "System" },
    };

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Summary = new List<string>(1) { "Builds a group of conditions(an expression) within the predicate(where clause) of a query." }
            };

            //Generate file header with information about the type.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            //using statements
            CSharpFile.UsingStatements(usingNamespaces);
            //open type with namespace
            CSharpFile.OpenClassWithNamespace(type);

            PropertyExtensions.Auto(TypeInfo.Int, "OpenExpressions", AccessModifiers.Internal);
            PropertyExtensions.AutoNoSetter(new TypeInfo(_operatorEnumType), "Operator", AccessModifiers.Internal);

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = typeName,
                AccessModifier = AccessModifiers.Public,
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "filterOperator", ParamType = _operatorEnumType } }
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("Operator = filterOperator");
            CSharpFile.CloseBlock();

            //close type and namespace
            CSharpFile.CloseTypeWithNamespace();
            //file footer
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

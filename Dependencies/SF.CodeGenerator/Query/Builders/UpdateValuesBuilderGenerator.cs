#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateUpdateValuesBuilder(ModelContainer modelContainer)
    {
        return UpdateValuesBuilder.Generate(modelContainer);
    }
    private static class UpdateValuesBuilder
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeName = Common.Names.UpdateValuesBuilder;

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Sealed,
                GenericParams = _genericParams,
                Summary = new List<string> { "Builds a list of fields and corresponding values to update. Used in UPDATE operations otherwise ignored." }
            };

            //Generate file header with information about the type.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            //using statements
            CSharpFile.UsingStatements(usingNamespaces);
            //open type with namespace
            CSharpFile.OpenClassWithNamespace(type);

            CSharpFile.Line($"private readonly {_genericQuery} _query");

            PropertyExtensions.AutoPrivateSetter(new TypeInfo($"List<{Common.Names.IQueryFilter}>"), "SetValues", AccessModifiers.Internal);

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = type.Name,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "query", ParamType = _genericQuery.Name},
                },
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_query = query");
            CSharpFile.CloseBlock();

            var method = new MethodDeclaration
            {
                Name = "Set",
                ReturnType = type.ReturnType,
                GenericParams = new GenericParameterList { new GenericParameter { Name = Common.Names.GenericValue } },
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "prop", ParamType = $"{Common.Names.Prop}<{_genericProp}, {Common.Names.GenericValue}>" },
                  new MethodParameter { Name = "val", ParamType = Common.Names.GenericValue }
                },
                Summary = new List<string> { "Adds a Set clause to the query. Used only for UPDATE operations. The value must be of the property's type." }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("SetValues == null");
            CSharpFile.LineIndented($"SetValues = new List<{Common.Names.IQueryFilter}>()");
            CSharpFile.Line($"SetValues.Add(prop.CreateFilter({Common.Names.FilterConditionsEnum}.Equals, {Common.Names.FilterOperatorsEnum}.And, val))");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "EndUpdateValues";
            method.ReturnType = new TypeInfo(_genericQuery);
            method.Parameters.Clear();
            method.GenericParams = null;
            method.Summary[0] = "Ends setting update values.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("_query");
            CSharpFile.CloseBlock();

            //close type and namespace
            CSharpFile.CloseTypeWithNamespace();
            //file footer
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

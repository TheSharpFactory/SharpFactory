#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateSortBuilder(ModelContainer modelContainer)
    {
        return SortBuilder.Generate(modelContainer);
    }
    private static class SortBuilder
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new ClassDeclaration
            {
                Name = Common.Names.SortBuilder,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Sealed,
                GenericParams = _genericParams,
                Summary = new List<string> { "Builds the sorting criteria for the query." }
            };

            //Generate file header with information about the type.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            //using statements
            CSharpFile.UsingStatements(usingNamespaces);
            //open type with namespace
            CSharpFile.OpenClassWithNamespace(type);

            CSharpFile.Line($"private readonly {_genericQuery} _query");

            PropertyExtensions.AutoPrivateSetter(new TypeInfo($"List<{Common.Names.ISortFilter}>"), "Sorting", AccessModifiers.Internal);

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = type.Name,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "query", ParamType = _genericQuery.Name },
                },
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_query = query");
            CSharpFile.CloseBlock();

            var method = new MethodDeclaration
            {
                Name = "OrderBy",
                ReturnType = type.ReturnType,
                GenericParams = new GenericParameterList { new GenericParameter { Name = Common.Names.GenericValue } },
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "prop", ParamType = $"{Common.Names.Prop}<{_genericProp}, {Common.Names.GenericValue}>" },
                },
                Summary = new List<string> { "Add order ascending by the specified property." }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("Sorting == null");
            CSharpFile.LineIndented($"Sorting = new List<{Common.Names.ISortFilter}>()");
            CSharpFile.Line($"Sorting.Add(prop.CreateSort({Common.Names.SortDirectionEnum}.Ascending))");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "OrderByDescending";
            method.Summary[0] = "Add order descending by the specified property.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("Sorting == null");
            CSharpFile.LineIndented($"Sorting = new List<{Common.Names.ISortFilter}>()");
            CSharpFile.Line($"Sorting.Add(prop.CreateSort({Common.Names.SortDirectionEnum}.Descending))");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "EndSorting";
            method.Parameters.Clear();
            method.GenericParams = null;
            method.ReturnType = new TypeInfo(_genericQuery);
            method.Summary[0] = "Ends composition of the sorting criteria for the query.";

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

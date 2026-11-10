#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateQueryBuilder(ModelContainer modelContainer)
    {
        return QueryBuilder.Generate(modelContainer);
    }
    private static class QueryBuilder
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(1)
            {
              new UsingNamespace { Name = "System" },
            };

            //Generate file header with information about the type.
            CSharpFile.Header(ArtifactKind.Class, Common.SharpFactoryFileMarker);

            CSharpFile.UsingStatements(usingNamespaces);

            //open namespace
            CSharpFile.OpenNamespace(typeNamespace);

            GenerateClass(typeNamespace);

            //close namespace
            CSharpFile.CloseNamespace();

            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
        private static void GenerateClass(string typeNamespace)
        {
            var predicateBuilder = Common.Names.PredicateBuilder;
            var updateValuesBuilder = Common.Names.UpdateValuesBuilder;
            var paginationBuilder = Common.Names.PaginationBuilder;
            var sortBuilder = Common.Names.SortBuilder;

            var type = new ClassDeclaration
            {
                Name = Common.Names.QueryBuilder,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Summary = new List<string> { "This class represents a query builder.  Used to correctly craft a query. NavProps is in NavPropQueryExtensions." },
                GenericParams = _genericParams
            };

            CSharpFile.OpenClass(type);

            PropertyExtensions.AutoPrivateSetter(new TypeInfo(_genericQuery), Common.Names.Query, AccessModifiers.Internal);

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.QueryBuilder,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "query", ParamType = _genericQuery.Name } },
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line($"{Common.Names.Query} = query");
            CSharpFile.CloseBlock();

            var method = new MethodDeclaration
            {
                Name = "Predicate",
                ReturnType = new TypeInfo(_genericQueryBuilder),
                Summary = new List<string> { "Compose the predicate(where clause)." },
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "builder", ParamType = $"Action<{predicateBuilder}<{_genericParams}>>" } }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"builder({Common.Names.Query}.BeginPredicate())");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "Sorting";
            method.Parameters[0].ParamType = $"Action<{sortBuilder}<{_genericParams}>>";
            method.Summary[0] = "Compose the sorting criteria.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"builder({Common.Names.Query}.BeginSorting())");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "Pagination";
            method.Parameters[0].ParamType = $"Action<{paginationBuilder}<{_genericParams}>>";
            method.Summary[0] = "Configure the pagination settings.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"builder({Common.Names.Query}.BeginPagination())");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "UpdateValues";
            method.Parameters[0].ParamType = $"Action<{updateValuesBuilder}<{_genericParams}>>";
            method.Summary[0] = "Set the fields and values to update.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"builder({Common.Names.Query}.BeginUpdateValues())");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }
    }
}

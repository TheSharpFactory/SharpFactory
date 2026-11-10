#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateQueryWrapper(ModelContainer modelContainer)
    {
        return QueryWrapper.Generate(modelContainer);
    }
    private static class QueryWrapper
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
                Name = Common.Names.QueryWrapper,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Sealed,
                GenericParams = _genericParams,
                Summary = new List<string> { "This class wraps the query to be usable by the Repository. Only used by the Repository." }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.Line($"private {_genericQuery} _query");
            PropertyExtensions.FullNoSetter(new TypeInfo($"List<{Common.Names.IQueryFilter}>"), "PredicateFilters", backingField: $"_query.{predicateBuilder}?.PredicateFilters");
            PropertyExtensions.FullNoSetter(new TypeInfo($"List<{Common.Names.IQueryFilter}>"), "SetValues", backingField: $"_query.{updateValuesBuilder}?.SetValues");
            PropertyExtensions.FullNoSetter(new TypeInfo(Common.Names.PaginationSettings), "Pagination", backingField: $"_query.{paginationBuilder}?.Pagination");
            PropertyExtensions.FullNoSetter(new TypeInfo($"List<{Common.Names.ISortFilter}>"), "Sorting", backingField: $"_query.{sortBuilder}?.Sorting");
            PropertyExtensions.FullNoSetter(new TypeInfo(Common.Names.NavPropCollection), "NavigationProperties", backingField: "_query.NavigationProperties");
            PropertyExtensions.AutoPrivateSetter(TypeInfo.Int, "OpenExpressions", AccessModifiers.Public);
            PropertyExtensions.AutoPrivateSetter(TypeInfo.Int, "ClosedExpressions", AccessModifiers.Public);
            PropertyExtensions.FullNoSetter(TypeInfo.Bool, "IsValid", backingField: "OpenExpressions == ClosedExpressions");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.QueryWrapper,
                AccessModifier = AccessModifiers.Public,
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "query", ParamType = _genericQuery.Name } },
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_query = query");
            CSharpFile.Line("CalculateOpenExpressions()");
            CSharpFile.Line("CalculateClosedExpressions()");
            CSharpFile.CloseBlock();

            var method = new MethodDeclaration
            {
                Name = "CalculateOpenExpressions"
            };
            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"(!(_query.{predicateBuilder}?.PredicateFilters?.Count > 0))");
            CSharpFile.ReturnIndented();
            CSharpFile.Line("var count = 0");
            CSharpFile.ForEach("f", $"_query.{predicateBuilder}.PredicateFilters");
            CSharpFile.LineIndented("count += f.StartExpressions");
            CSharpFile.Line("OpenExpressions = count");
            CSharpFile.CloseBlock();

            method.Name = "CalculateClosedExpressions";
            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"!(_query.{predicateBuilder}?.PredicateFilters?.Count > 0)");
            CSharpFile.ReturnIndented();
            CSharpFile.Line("var count = 0");
            CSharpFile.ForEach("f", $"_query.{predicateBuilder}.PredicateFilters");
            CSharpFile.LineIndented("count += f.EndExpressions");
            CSharpFile.Line("ClosedExpressions = count");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }
    }
}

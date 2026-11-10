#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateQueryClass(ModelContainer modelContainer)
    {
        return QueryClass.Generate(modelContainer);
    }
    private static class QueryClass
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
            var queryBuilder = Common.Names.QueryBuilder;

            var type = new ClassDeclaration
            {
                Name = Common.Names.Query,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Sealed,
                GenericParams = _genericParams,
                Summary = new List<string> { "This class represents all aspects of a query sent to the Repository." }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.Line($"private {Common.Names.QueryWrapper}<{_genericParams}> _wrapper");
            PropertyExtensions.AutoPrivateSetter(new TypeInfo($"{predicateBuilder}<{_genericParams}>"), predicateBuilder, AccessModifiers.Internal);
            PropertyExtensions.AutoPrivateSetter(new TypeInfo($"{updateValuesBuilder}<{_genericParams}>"), updateValuesBuilder, AccessModifiers.Internal);
            PropertyExtensions.AutoPrivateSetter(new TypeInfo($"{paginationBuilder}<{_genericParams}>"), paginationBuilder, AccessModifiers.Internal);
            PropertyExtensions.AutoPrivateSetter(new TypeInfo($"{sortBuilder}<{_genericParams}>"), sortBuilder, AccessModifiers.Internal);
            PropertyExtensions.Auto(new TypeInfo("object"), Common.Names.NavPropsBuilder, AccessModifiers.Internal);
            PropertyExtensions.Auto(new TypeInfo(Common.Names.NavPropCollection), "NavigationProperties", AccessModifiers.Internal);
            PropertyExtensions.AutoPrivateSetter(new TypeInfo(_genericQueryBuilder), queryBuilder, AccessModifiers.Internal);

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.Query,
                AccessModifier = AccessModifiers.Public,
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line($"_wrapper = new {Common.Names.QueryWrapper}<{_genericParams}>(this)");
            CSharpFile.CloseBlock();

            PropertyExtensions.FullNoSetter(TypeInfo.Double, "RecordCount", backingField: $"{paginationBuilder}?.Pagination != null ? {paginationBuilder}.Pagination.TotalCount : 0D", description: "Only works when ReturnTotalCount is true. Gets the total number of records found for this query. This is the count that would be returned without pagination.");
            PropertyExtensions.FullNoSetter(TypeInfo.Int, "PageCount", backingField: $"{paginationBuilder}?.Pagination != null ? {paginationBuilder}.Pagination.PageCount : 0", description: "Only works when ReturnTotalCount is true. Gets the total number of pages found for this query.");

            var method = new MethodDeclaration
            {
                Name = "BeginPredicate",
                ReturnType = new TypeInfo($"{predicateBuilder}<{_genericParams}>"),
                Summary = new List<string> { "Begin composing the predicate(where clause)." }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"{predicateBuilder} == null");
            CSharpFile.Line($"{predicateBuilder} = new {predicateBuilder}<{_genericParams}>(this)");
            CSharpFile.Return(predicateBuilder);
            CSharpFile.CloseBlock();

            method.Name = "BeginUpdateValues";
            method.ReturnType = new TypeInfo($"{updateValuesBuilder}<{_genericParams}>");
            method.Summary[0] = "Begin setting the fields and values to update.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"{updateValuesBuilder} == null");
            CSharpFile.LineIndented($"{updateValuesBuilder} = new {updateValuesBuilder}<{_genericParams}>(this)");
            CSharpFile.Return(updateValuesBuilder);
            CSharpFile.CloseBlock();

            method.Name = "BeginPagination";
            method.ReturnType = new TypeInfo($"{paginationBuilder}<{_genericParams}>");
            method.Summary[0] = "Begin configuring pagination settings.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"{paginationBuilder} == null");
            CSharpFile.LineIndented($"{paginationBuilder} = new {paginationBuilder}<{_genericParams}>(this)");
            CSharpFile.Return(paginationBuilder);
            CSharpFile.CloseBlock();

            method.Name = "BeginSorting";
            method.ReturnType = new TypeInfo($"{sortBuilder}<{_genericParams}>");
            method.Summary[0] = "Begin adding the fields to sort the query by.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"{sortBuilder} == null");
            CSharpFile.LineIndented($"{sortBuilder} = new {sortBuilder}<{_genericParams}>(this)");
            CSharpFile.Return(sortBuilder);
            CSharpFile.CloseBlock();

            method.Name = "Builder";
            method.ReturnType = new TypeInfo(_genericQuery);
            method.Summary[0] = "Use the query builder to craft your query.";
            method.Parameters = new List<MethodParameter> { new MethodParameter { Name = "builder", ParamType = $"Action<{_genericQueryBuilder}>" } };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"{queryBuilder} == null");
            CSharpFile.LineIndented($"{queryBuilder} = new {_genericQueryBuilder}(this)");
            CSharpFile.Line($"builder({queryBuilder})");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "Prepare";
            method.ReturnType = new TypeInfo($"{Common.Names.QueryWrapper}<{_genericParams}>");
            method.Summary[0] = "This method is for internal use only and is not intended to be used directly in your code.";
            method.Parameters = null;

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("_wrapper");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }
    }
}

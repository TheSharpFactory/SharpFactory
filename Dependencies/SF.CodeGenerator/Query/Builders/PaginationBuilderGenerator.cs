#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GeneratePaginationBuilder(ModelContainer modelContainer)
    {
        return PaginationBuilder.Generate(modelContainer);
    }
    private static class PaginationBuilder
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeName = Common.Names.PaginationBuilder;

            //var paginationBuilderType = $"{typeName}<{_genericParams}>";
            //var typeDeclaration = $"public sealed class {paginationBuilderType} where {_genericProp} : struct, IComparable, IFormattable, IConvertible where {_genericQuery} : {Common.Names.QueryBase()}<{_genericParams}>, new() where {_genericQueryBuilder} : {Common.Names.QueryBuilderBase()}<{_genericParams}>, new()";

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(1)
            {
              new UsingNamespace { Name = "System" },
            };

            // internal static readonly string _genericParams = $"{_genericProp}, {_genericQuery}, {_genericQueryBuilder}";
            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                IsPartial = false,
                Summary = new List<string> { "Builds the pagination settings for the query." },
                GenericParams = _genericParams
            };

            //Generate file header with information about the type.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            //using statements
            CSharpFile.UsingStatements(usingNamespaces);
            //open type with namespace
            CSharpFile.OpenClassWithNamespace(type);

            CSharpFile.Field(new FieldDeclaration { Name = "_query", ReturnType = new TypeInfo(_genericQuery.Name), AccessModifier = AccessModifiers.Private, IsReadonly = true });

            PropertyExtensions.AutoPrivateSetter(new TypeInfo($"{Common.Names.PaginationSettings}"), "Pagination", AccessModifiers.Internal);

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = typeName,
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
                Name = "PageSize",
                ReturnType = type.ReturnType,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter{ Name ="size", ParamType = "int" }
                },
                Summary = new List<string> { "Set the page size for the query." }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("Pagination == null");
            CSharpFile.LineIndented($"Pagination = new {Common.Names.PaginationSettings}()");
            CSharpFile.Line("Pagination.PageSize = size");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "PageNumber";
            method.Parameters[0].Name = "number";
            method.Summary[0] = "Set the page number for the query.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("Pagination == null");
            CSharpFile.LineIndented($"Pagination = new {Common.Names.PaginationSettings}()");
            CSharpFile.Line("Pagination.PageNumber = number");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "ReturnTotalCount";
            method.Parameters[0].ParamType = "bool";
            method.Parameters[0].Name = "getCount";
            method.Summary[0] = "If true returns the total record count. Used for pagination.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("Pagination == null");
            CSharpFile.Line($"Pagination = new {Common.Names.PaginationSettings}()");
            CSharpFile.Line("Pagination.GetTotalCount = getCount");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "EndPagination";
            method.Parameters.Clear();
            method.ReturnType = new TypeInfo(_genericQuery);
            method.Summary[0] = "Ends the pagination configuration.";

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

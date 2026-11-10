#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateSortFilter(ModelContainer modelContainer)
    {
        return SortFilter.Generate(modelContainer);
    }
    public static string GenerateSortFilterInterface(ModelContainer modelContainer)
    {
        return SortFilter.GenerateInterface(modelContainer);
    }
    private static class SortFilter
    {
        public static string GenerateInterface(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            //var typeDeclaration = $"public partial interface {Common.Names.ISortFilter()}";

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
            var usingNamespaces = new List<UsingNamespace>(1)
            {
              new UsingNamespace { Name = "System" },
            };

            var summary = "This interface represents a sorting filter used when calling the Repository.";

            var type = new InterfaceDeclaration
            {
                Name = Common.Names.ISortFilter,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string> { summary }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenInterfaceWithNamespace(type);
            //Generate file header with information about the class.  

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Int, "PropertyId", "Gets the id of the property to sort by.");
            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.String, "PropertyName", "Gets the name of the property to sort by.");
            PropertyExtensions.InterfaceAutoNoSetter(new TypeInfo(_sortDirectionEnumType), _sortDirectionEnumType, "Gets the sort direction to apply to the sort.");

            CSharpFile.EmptyLine();
            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeName = Common.Names.SortFilter;
            var sortDirectionEnumName = Common.Names.SortDirectionEnum;

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
                IsStatic = true,
                Summary = new List<string> { "This class is a factory for creating SortFilter instances." }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);

            CSharpFile.OpenClassWithNamespace(type);

            var method = new MethodDeclaration
            {
                Name = "New",
                ReturnType = new TypeInfo(Common.Names.ISortFilter),
                IsStatic = true,
                GenericParams = new GenericParameterList { _genericProp, new GenericParameter { Name = Common.Names.GenericValue } },
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "prop", ParamType = $"{Common.Names.Prop}<{_genericProp}, {Common.Names.GenericValue}>", Description = "The property." },
                   new MethodParameter { Name = "sortdirection", ParamType =sortDirectionEnumName, Description = "The sort direction." },
                },
                Summary = new List<string> { "Returns a new SortFilter on a property of a container." }
            };
            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("prop.CreateSort(sortdirection)");
            CSharpFile.CloseBlock();

            method.Parameters.RemoveAt(1);
            method.Summary[0] = $"Returns a new SortFilter on a property of a container. Using the {sortDirectionEnumName}.Ascending";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"prop.CreateSort({sortDirectionEnumName}.Ascending)");
            CSharpFile.CloseBlock();
            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

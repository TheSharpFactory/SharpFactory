#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateOperatorsEnum(ModelContainer modelContainer)
    {
        return OperatorsEnum.Generate(modelContainer);
    }
    public static string GenerateFilterConditionsEnum(ModelContainer modelContainer)
    {
        return FilterConditionsEnum.Generate(modelContainer);
    }
    public static string GenerateSortDirectionEnum(ModelContainer modelContainer)
    {
        return SortDirectionEnum.Generate(modelContainer);
    }

    private static class OperatorsEnum
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeName = Common.Names.FilterOperatorsEnum;
            //var typeDeclaration = "public enum " + typeName;

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
            //var summary = "This enum represents the operators to chain the filter into the query.";

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new EnumDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                Summary = new List<string> { "This enum represents the operators to chain the filter into the query." }
            };

            //Generate file header with information about the class.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenEnumWithNamespace(type);

            var enumMember = new EnumMemberDeclaration { Name = "And" };
            CSharpFile.EnumMember(enumMember);

            enumMember.Name = "Or";
            CSharpFile.EnumMember(enumMember);

            CSharpFile.CloseType();

            //here create the equality comparer to be used with all dictionaries and hashsets
            //EqualityComparerGenerator.Generate(modelContainer.CommonSettings, typeName);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
    private static class FilterConditionsEnum
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeName = Common.Names.FilterConditionsEnum;
            //var typeDeclaration = "public enum " + typeName;

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
            //var summary = "This enum represents the filter conditions to use when calling the Repository.";
            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new EnumDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                Summary = new List<string> { "This enum represents the filter conditions to use when calling the Repository." }
            };

            //Generate file header with information about the class.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenEnumWithNamespace(type);

            var conditions = Common._supportedFilterConditions;
            var filterCount = conditions.Length;

            var enumMember = new EnumMemberDeclaration();
            for(var i = 0; i < filterCount; i++)
            {
                enumMember.Name = conditions[i];
                CSharpFile.EnumMember(enumMember);
            }

            CSharpFile.CloseType();

            //here create the equality comparer to be used with all dictionaries and hashsets
            //EqualityComparerGenerator.Generate(modelContainer.CommonSettings, typeName);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
    private static class SortDirectionEnum
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeName = Common.Names.SortDirectionEnum;
            //var typeDeclaration = "public enum " + Common.Names.SortDirectionEnum();

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
            //var summary = "This enum represents the possible sorting directions for results from the Repository.";
            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new EnumDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                Summary = new List<string> { "This enum represents the possible sorting directions for results from the Repository." }
            };

            //Generate file header with information about the class.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenEnumWithNamespace(type);

            var enumMember = new EnumMemberDeclaration { Name = "Ascending" };
            CSharpFile.EnumMember(enumMember);

            enumMember.Name = "Descending";
            CSharpFile.EnumMember(enumMember);

            CSharpFile.CloseType();

            //here create the equality comparer to be used with all dictionaries and hashsets
            //EqualityComparerGenerator.Generate(modelContainer.CommonSettings, typeName);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
}

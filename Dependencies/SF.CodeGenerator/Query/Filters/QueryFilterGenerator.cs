#region Usings
using System;
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateQueryFilter(ModelContainer modelContainer)
    {
        return QueryFilter.Generate(modelContainer);
    }
    public static string GenerateQueryFilterInterface(ModelContainer modelContainer)
    {
        return QueryFilter.GenerateInterface(modelContainer);
    }
    private static class QueryFilter
    {
        public static string GenerateInterface(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = typeNamespace }
            };

            var type = new InterfaceDeclaration
            {
                Name = Common.Names.IQueryFilter,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string> { "This interface represents a filter used when calling the Repository." }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenInterfaceWithNamespace(type);
            //Generate Properties
            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Int, "PropertyId", "Gets the id of the property to filter by.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.String, "PropertyName", "Gets the name of the property to filter by.");

            PropertyExtensions.InterfaceAutoNoSetter(new TypeInfo(_conditionEnumType), "Condition", "Gets the condition to apply to the filter.");

            PropertyExtensions.InterfaceAutoNoSetter(new TypeInfo(_operatorEnumType), "Operator", "Gets the operator used to chain the filter to the query.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.String, "TypeName", "Gets the name of the CLR type corresponding to the Property specified in the FieldName.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Type, "Type", "Gets the name of the CLR type corresponding to the Property specified in the FieldName.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.TypeCode, "TypeCode", "Gets System.TypeCode that corresponds to Type.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Bool, "IsArray", "True if the Value property is an Array.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Bool, "IsDateFilter", "True if filtering by the Date part of a DateTime.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Bool, "IsYearFilter", "True if filtering by the Date.Year part of a DateTime.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Bool, "IsMonthFilter", "True if filtering by the Date.Month part of a DateTime.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Bool, "IsDayFilter", "True if filtering by the Date.Day part of a DateTime.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Bool, "IsNullable", "True if the property is nullable.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Bool, "IsBoolean", "True if the Value property is a boolean.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Bool, "HasValue", "True if the value is not null.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Object, "Value", "Gets the value to use for filtering.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Int, "StartExpressions", "Gets the number of expressions that start on this filter.  Used for grouping conditions inside parenthesis.");

            PropertyExtensions.InterfaceAutoNoSetter(TypeInfo.Int, "EndExpressions", "Gets the number of expressions that end on this filter.  Used for grouping conditions inside parenthesis.");

            CSharpFile.CloseType();

            //the same filter, of a property of TProps: what GetChanges, CreateChange and QueryFilter.New give, and what
            //an update of TProps takes
            CSharpFile.OpenInterface(new InterfaceDeclaration
            {
                Name = Common.Names.IQueryFilter,
                Namespace = typeNamespace,
                GenericParams = new GenericParameterList { _genericProp },
                Interfaces = new List<string> { Common.Names.IQueryFilter },
                Summary = new List<string> { "A filter on a property of TProps." }
            });
            CSharpFile.CloseType();

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(3)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
              new UsingNamespace { Name = typeNamespace }
            };

            var type = new ClassDeclaration
            {
                Name = Common.Names.QueryFilter,
                Namespace = typeNamespace,
                IsStatic = true,
                Summary = new List<string>(1) { "This class is a factory for creating QueryFilter instances." }
            };

            //Generate file header with information about the type.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            var method = new MethodDeclaration
            {
                Name = "New",
                IsStatic = true,
                GenericParams = new GenericParameterList { _genericProp, new GenericParameter { Name = Common.Names.GenericValue } },
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "prop", ParamType = $"{Common.Names.Prop}<{_genericProp}, {Common.Names.GenericValue}>", Description = "The property to filter on." },
                  new MethodParameter { Name = "condition", ParamType = _conditionEnumType, Description = "The condition to apply to the filter." },
                  new MethodParameter { Name = "val", ParamType = Common.Names.GenericValue, Description = "The value to filter by." },
                },
                ReturnType = new TypeInfo($"{Common.Names.IQueryFilter}<{Common.Names.GenericProp}>"),
                Summary = new List<string> { "Initializes a new filter on a property of a container. The value must be of the property's type." }
            };
            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"prop.CreateFilter(condition, {_operatorEnumType}.And, val)");
            CSharpFile.CloseBlock();

            method.Parameters.Insert(2, new MethodParameter { Name = "op", ParamType = _operatorEnumType, Description = "The operator to chain conditions. For example And, Or." });
            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("prop.CreateFilter(condition, op, val)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

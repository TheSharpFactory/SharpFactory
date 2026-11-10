#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateFilterBuilder(ModelContainer modelContainer)
    {
        return FilterBuilder.Generate(modelContainer);
    }
    private static class FilterBuilder
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

            //using statements
            CSharpFile.UsingStatements(usingNamespaces);

            CSharpFile.OpenNamespace(typeNamespace);

            GenerateQueryFilterBuilder();

            CSharpFile.CloseNamespace();

            //file footer
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }

        private static readonly string _predicateBuilder = $"{Common.Names.PredicateBuilder}<{_genericParams}>";

        private static void GenerateQueryFilterBuilder()
        {
            var type = new ClassDeclaration
            {
                Name = Common.Names.FilterBuilder,
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Summary = new List<string>
                {
                    "Builds a filter for the predicate(where clause) of the query on a property of a known type.",
                    "The conditions only some types have are in FilterBuilderExtensions."
                },
                GenericParams = new GenericParameterList
                {
                    _genericProp,
                    new GenericParameter { Name = Common.Names.GenericValue }
                }
            };

            CSharpFile.OpenClass(type);

            //first generate private fields
            CSharpFile.Line($"private readonly {Common.Names.Prop}<{_genericProp}, {Common.Names.GenericValue}> _property");
            CSharpFile.Line($"private readonly {_predicateBuilder} _predicateBuilder");
            CSharpFile.Line($"private readonly {_operatorEnumType} _operator");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.FilterBuilder,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "property", ParamType = $"{Common.Names.Prop}<{_genericProp}, {Common.Names.GenericValue}>" },
                  new MethodParameter { Name = "filterOperator", ParamType = _operatorEnumType },
                  new MethodParameter { Name = "predicateBuilder", ParamType = _predicateBuilder },
                },
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_property = property");
            CSharpFile.Line("_operator = filterOperator");
            CSharpFile.Line("_predicateBuilder = predicateBuilder");
            CSharpFile.CloseBlock();

            CSharpFile.OpenRegion("Equality");
            GenerateEqualityMethod("Equals", "Equals", "is equals");
            GenerateEqualityMethod("NotEquals", "NotEquals", "is not equals");
            GenerateHiddenObjectEquals();
            CSharpFile.CloseRegion();

            var method = new MethodDeclaration
            {
                Name = "AddFilter",
                AccessModifier = AccessModifiers.Internal,
                ReturnType = new TypeInfo(_predicateBuilder),
                ReturnTypeForComments = "PredicateBuilder",
                Summary = new List<string> { "Adds the filter to the predicate with the operator this builder was started with (AND or OR)." },
                Parameters = new List<MethodParameter>(2)
                {
                  new MethodParameter { Name = "condition", ParamType = _conditionEnumType, Description = "The condition to use by the filter." },
                  new MethodParameter { Name = "val", ParamType = "object", Description = "The value to filter by." }
                }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"_predicateBuilder.AddCondition(_property, _operator, condition, val)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }

        /// <summary>
        /// Hides object.Equals(object): without it a value of the wrong type would call object.Equals, compile, and
        /// add no filter. With it, the compiler reports the wrong type.
        /// </summary>
        private static void GenerateHiddenObjectEquals()
        {
            var method = new MethodDeclaration
            {
                Name = "Equals",
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.New,
                ReturnType = TypeInfo.Bool,
                ReturnTypeForComments = "bool. Never call this method: the compiler rejects every call to it (Obsolete as an error).",
                Attributes = new List<string>(1) { "Obsolete(\"The value is not of the property's type. Pass a value of the type the property declares.\", true)" },
                Summary = new List<string> { "Not for use: it stops a value of the wrong type from reaching object.Equals.", "Pass a value of the property's type to Equals." },
                Parameters = new List<MethodParameter>(1) { new MethodParameter { Name = "val", ParamType = "object", Description = "A value that is not of the property's type." } }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("base.Equals(val)");
            CSharpFile.CloseBlock();
        }

        private static void GenerateEqualityMethod(string name, string condition, string descCondition)
        {
            var method = new MethodDeclaration
            {
                Name = name,
                AccessModifier = AccessModifiers.Public,
                ReturnType = new TypeInfo(_predicateBuilder),
                ReturnTypeForComments = "PredicateBuilder",
                Summary = new List<string> { "Add a filter to the predicate(where clause).", $"Where the Property {descCondition} the passed in value." },
                Parameters = new List<MethodParameter>(1) { new MethodParameter { Name = "val", ParamType = Common.Names.GenericValue, Description = "The value to filter by." } }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"AddFilter({_conditionEnumType}.{condition}, val)");
            CSharpFile.CloseBlock();
        }
    }
}

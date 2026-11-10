#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GeneratePredicateBuilder(ModelContainer modelContainer)
    {
        return PredicateBuilder.Generate(modelContainer);
    }
    private static class PredicateBuilder
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var predicateFiltersPropName = "PredicateFilters";
            var filterConditionsEnum = Common.Names.FilterConditionsEnum;
            var filterOperatorsEnum = Common.Names.FilterOperatorsEnum;
            var expressionBuilder = Common.Names.ExpressionBuilder;
            var predicateBuilder = Common.Names.PredicateBuilder;
            var filterBuilder = Common.Names.FilterBuilder;

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new ClassDeclaration
            {
                Name = predicateBuilder,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Sealed,
                GenericParams = _genericParams,
                Summary = new List<string> { "Builds the predicate(where clause) of a query." }
            };

            //Generate file header with information about the type.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            //using statements
            CSharpFile.UsingStatements(usingNamespaces);
            //open type with namespace
            CSharpFile.OpenClassWithNamespace(type);

            CSharpFile.Field(new FieldDeclaration { Name = "_query", ReturnType = new TypeInfo(_genericQuery), AccessModifier = AccessModifiers.Private, IsReadonly = true });

            PropertyExtensions.Auto(new TypeInfo($"{expressionBuilder}"), "CurrentExpression", AccessModifiers.Internal, description: $"Gets or sets the current {expressionBuilder} being composed in the query.");
            PropertyExtensions.AutoPrivateSetter(new TypeInfo($"List<{Common.Names.IQueryFilter}>"), predicateFiltersPropName, AccessModifiers.Internal, description: $"Gets the filters of the query.");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = predicateBuilder,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "query", ParamType = _genericQuery.Name },
                },
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_query = query");
            CSharpFile.CloseBlock();

            CSharpFile.OpenRegion("Expression Builder");

            var method = new MethodDeclaration
            {
                Name = "Where",
                ReturnType = new TypeInfo($"{predicateBuilder}<{_genericParams}>"),
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "builder", ParamType = $"Action<{predicateBuilder}<{_genericParams}>>" }
                },
                Summary = new List<string> { "Uses AND operator. Adds a group of conditions to be evaluated together as a single expression." }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line("builder(BeginExpression())");
            CSharpFile.Line("EndExpression()");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "Or";
            method.Summary[0] = "Uses OR operator. Adds a group of conditions to be evaluated together as a single expression.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line("builder(OrBeginExpression())");
            CSharpFile.Line("EndExpression()");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "BeginExpression";
            method.Parameters.Clear();
            method.Summary[0] = "Initiates a new expression(group of filters) using the AND operator.";
            method.AccessModifier = AccessModifiers.Private;

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("CurrentExpression == null");
            CSharpFile.Line($"CurrentExpression = new {expressionBuilder}({_operatorEnumType}.And)");
            CSharpFile.Line("CurrentExpression.OpenExpressions++");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "OrBeginExpression";
            method.Parameters.Clear();
            method.Summary[0] = "Initiates a new expression(group of filters) using the OR operator.";
            method.AccessModifier = AccessModifiers.Private;

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("CurrentExpression == null");
            CSharpFile.Line($"CurrentExpression = new {expressionBuilder}({_operatorEnumType}.Or)");
            CSharpFile.Line("CurrentExpression.OpenExpressions++");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "EndExpression";
            method.Parameters.Clear();
            method.Summary[0] = "Ends an expression. This method must be called once per every Begin Expression call.";
            method.AccessModifier = AccessModifiers.Private;

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"{predicateFiltersPropName}?.Count > 0");
            CSharpFile.LineIndented($"(({Common.Names.PropertyFilter}){predicateFiltersPropName}[{predicateFiltersPropName}.Count - 1]).EndExpressions++");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Fluid Syntax");

            method.Name = "Where";
            method.ReturnType = new TypeInfo($"{filterBuilder}<{_genericProp}, {Common.Names.GenericValue}>");
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { Name = "prop", ParamType = $"{Common.Names.Prop}<{_genericProp}, {Common.Names.GenericValue}>", Description = "The property, with the type of its values." });
            method.Summary[0] = "Begins condition: Uses AND operator. The filter takes only values of the property's type.";
            method.AccessModifier = AccessModifiers.Public;
            method.GenericParams = new GenericParameterList { new GenericParameter { Name = Common.Names.GenericValue } };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"new {filterBuilder}<{_genericProp}, {Common.Names.GenericValue}>(prop, {filterOperatorsEnum}.And, this)");
            CSharpFile.CloseBlock();

            method.Name = "Or";
            method.Summary[0] = "Begins condition: Uses OR operator. The filter takes only values of the property's type.";

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"new {filterBuilder}<{_genericProp}, {Common.Names.GenericValue}>(prop, {filterOperatorsEnum}.Or, this)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseRegion();

            method.Name = "EndPredicate";
            method.ReturnType = new TypeInfo(_genericQuery);
            method.Parameters.Clear();
            method.Summary[0] = "Ends predicate(where clause).";
            method.GenericParams = null;

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("_query");
            CSharpFile.CloseBlock();

            method.Name = "AddCondition";
            method.AccessModifier = AccessModifiers.Internal;
            method.ReturnType = new TypeInfo($"{predicateBuilder}<{_genericParams}>");
            method.Parameters.Clear();
            method.GenericParams = new GenericParameterList { new GenericParameter { Name = Common.Names.GenericValue } };
            method.Parameters.Add(new MethodParameter { Name = "prop", ParamType = $"{Common.Names.Prop}<{_genericProp}, {Common.Names.GenericValue}>" });
            method.Parameters.Add(new MethodParameter { Name = "filterOperator", ParamType = filterOperatorsEnum });
            method.Parameters.Add(new MethodParameter { Name = "condition", ParamType = filterConditionsEnum });
            method.Parameters.Add(new MethodParameter { Name = "val", ParamType = "object" });
            method.Summary.Clear();

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If($"{predicateFiltersPropName} == null");
            CSharpFile.LineIndented($"{predicateFiltersPropName} = new List<{Common.Names.IQueryFilter}>()");
            CSharpFile.If("CurrentExpression == null");
            CSharpFile.OpenBlock();
            CSharpFile.Line($"{predicateFiltersPropName}.Add(prop.CreateFilter(condition, filterOperator, val))");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            CSharpFile.Line($"var filter = prop.CreateFilter(condition, CurrentExpression.Operator, val)");
            CSharpFile.Line("filter.StartExpressions = CurrentExpression.OpenExpressions");
            CSharpFile.Line($"{predicateFiltersPropName}.Add(filter)");
            CSharpFile.Line("CurrentExpression = null");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            //close type and namespace
            CSharpFile.CloseTypeWithNamespace();
            //file footer
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

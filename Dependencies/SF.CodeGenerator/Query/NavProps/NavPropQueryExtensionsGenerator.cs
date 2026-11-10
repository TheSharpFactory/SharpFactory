#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateNavPropQueryExtensions(ModelContainer modelContainer)
    {
        return NavPropQueryExtensionsClass.Generate(modelContainer);
    }

    private static class NavPropQueryExtensionsClass
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            CSharpFile.Header(ArtifactKind.Class, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(new List<UsingNamespace>(1) { new UsingNamespace { Name = "System" } });
            CSharpFile.OpenNamespace(typeNamespace);

            var type = new ClassDeclaration
            {
                Name = Common.Names.NavPropQueryExtensions,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                IsStatic = true,
                Summary = new List<string>
                {
                    "The navigation property members, as extension methods constrained to NavPropertyContainer: a query on an entity without navigation properties does not have them, and Then does not exist on a path standing on such an entity."
                }
            };

            CSharpFile.OpenClass(type);

            var method = new MethodDeclaration
            {
                Name = "BeginNavProps",
                AccessModifier = AccessModifiers.Public,
                IsStatic = true,
                ReturnType = new TypeInfo($"{Common.Names.NavPropsBuilder}<{_genericProp}>"),
                GenericParams = new GenericParameterList { _genericNavProp },
                Parameters = new List<MethodParameter>(1)
                {
                    new MethodParameter { Name = "query", ParamType = "this " + _genericQuery }
                },
                Summary = new List<string> { "Begin composing the navigation property tree." }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("query.NavPropsBuilder == null");
            CSharpFile.LineIndented($"query.NavPropsBuilder = new {Common.Names.NavPropsBuilder}<{_genericProp}>(query)");
            CSharpFile.Return($"({Common.Names.NavPropsBuilder}<{_genericProp}>)query.NavPropsBuilder");
            CSharpFile.CloseBlock();

            method.Name = Common.Names.NavPropCollection;
            method.ReturnType = new TypeInfo(_genericQueryBuilder);
            method.GenericParams = new GenericParameterList { _genericNavProp };
            method.Parameters = new List<MethodParameter>(2)
            {
                new MethodParameter { Name = "builder", ParamType = "this " + _genericQueryBuilder },
                new MethodParameter { Name = "navProps", ParamType = $"Action<{Common.Names.NavPropsBuilder}<{_genericProp}>>" }
            };
            method.Summary = new List<string> { "Compose the navigation properties." };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"navProps(builder.{Common.Names.Query}.BeginNavProps())");
            CSharpFile.Return("builder");
            CSharpFile.CloseBlock();

            method.Name = "Then";
            method.ReturnType = new TypeInfo($"{Common.Names.NavPathBuilder}<{_genericProp.Name}, TNext>");
            method.ReturnTypeForComments = "NavPathBuilder";
            method.GenericParams = new GenericParameterList
            {
                _genericNavProp,
                new GenericParameter { Name = "TCurrent", Constraint = Common.Names.NavPropertyContainer },
                new GenericParameter { Name = "TNext", Constraint = Common.Names.PropertyContainer }
            };
            method.Parameters = new List<MethodParameter>(2)
            {
                new MethodParameter { Name = "path", ParamType = $"this {Common.Names.NavPathBuilder}<{_genericProp.Name}, TCurrent>" },
                new MethodParameter { Name = "prop", ParamType = $"{Common.Names.NavProp}<TCurrent, TNext>" }
            };
            method.Summary = new List<string> { "Add a Sub Navigation Property to the current level of the query: only one of the entity the path stands on." };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line("path.Root.Then(prop.EnumValue, prop.Name, typeof(TCurrent))");
            CSharpFile.Return($"new {Common.Names.NavPathBuilder}<{_genericProp.Name}, TNext>(path.Root)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

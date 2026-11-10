#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateNavPropsBuilder(ModelContainer modelContainer)
    {
        return NavPropsBuilder.Generate(modelContainer);
    }
    private static class NavPropsBuilder
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(1)
            {
              new UsingNamespace { Name = "System" },
            };

            var type = new ClassDeclaration
            {
                Name = Common.Names.NavPropsBuilder,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Summary = new List<string> { "Builds the navigation property tree for a query. Exists only for containers of entities with navigation properties." },
                GenericParams = _genericParamsWithNavProps
            };

            //Generate file header with information about the type.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            //using statements
            CSharpFile.UsingStatements(usingNamespaces);
            //open type with namespace
            CSharpFile.OpenClassWithNamespace(type);

            CSharpFile.Line($"private readonly {_genericQuery} _query");
            CSharpFile.Line($"private {Common.Names.NavProp} _current = null");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.NavPropsBuilder,
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
                Name = "Append",
                AccessModifier = AccessModifiers.Public,
                ReturnType = new TypeInfo($"{Common.Names.NavPathBuilder}<{_genericProp.Name}, TTarget>"),
                ReturnTypeForComments = "NavPathBuilder",
                GenericParams = new GenericParameterList { new GenericParameter { Name = "TTarget", Constraint = Common.Names.PropertyContainer } },
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "prop", ParamType = $"{Common.Names.NavProp}<{_genericProp}, TTarget>", Description = "Navigation Property" } },
                Summary = new List<string> { "Add a Navigation Property to the root level of the query. Then continues from the entity it leads to." }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("_query.NavigationProperties == null");
            CSharpFile.LineIndented($"_query.NavigationProperties = new {Common.Names.NavPropCollection}()");
            CSharpFile.Line("var newNavProp = prop.CreateItem()");
            CSharpFile.If("_current != null && _current.IntValue == newNavProp.IntValue");
            CSharpFile.Return($"new {Common.Names.NavPathBuilder}<{_genericProp.Name}, TTarget>(this)");
            CSharpFile.Line("_current = newNavProp");
            CSharpFile.Line("_query.NavigationProperties.Add(newNavProp)");
            CSharpFile.Return($"new {Common.Names.NavPathBuilder}<{_genericProp.Name}, TTarget>(this)");
            CSharpFile.CloseBlock();

            method.Name = "Append";
            method.GenericParams = null;
            method.ReturnType = new TypeInfo($"{Common.Names.NavPropsBuilder}<{_genericParams}>");
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { Name = "props", ParamType = $"{Common.Names.NavPropCollection}<{_genericParams}>", Description = "Navigation Property Collection" });
            method.Summary = new List<string> { "Add a Navigation Property Collection to the root level of the query." };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("!(props?.Items.Count > 0)");
            CSharpFile.Return("this");
            CSharpFile.If("_query.NavigationProperties == null");
            CSharpFile.LineIndented($"_query.NavigationProperties = new {Common.Names.NavPropCollection}()");
            CSharpFile.ForEach("np", "props.Items");
            CSharpFile.Line("_query.NavigationProperties.Add(np)");
            CSharpFile.Return("this");
            CSharpFile.CloseBlock();

            method.Name = "EndNavProps";
            method.Parameters.Clear();
            method.Summary = new List<string> { "Ends setting the navigation properties." };
            method.ReturnType = new TypeInfo(_genericQuery);

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("_query");
            CSharpFile.CloseBlock();

            method.Name = "Then";
            method.AccessModifier = AccessModifiers.Internal;
            method.ReturnType = TypeInfo.Void;
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { Name = "id", ParamType = "int" });
            method.Parameters.Add(new MethodParameter { Name = "name", ParamType = "string" });
            method.Parameters.Add(new MethodParameter { Name = "owner", ParamType = "Type" });
            method.Summary = new List<string> { "Add a Sub Navigation Property to the current level of the query." };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("_query.NavigationProperties == null");
            CSharpFile.LineIndented("throw new InvalidOperationException(\"No navigation properties found to attach a sub navigation property.\")");
            CSharpFile.If("_current == null");
            CSharpFile.LineIndented("throw new InvalidOperationException(\"No navigation properties found to attach a sub navigation property.\")");
            CSharpFile.Line("_current = _current.SubNavProp(id, name, owner)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();

            GenerateNavPathBuilder(typeNamespace);

            //close namespace
            CSharpFile.CloseNamespace();
            //file footer
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }

        private static void GenerateNavPathBuilder(string typeNamespace)
        {
            var type = new ClassDeclaration
            {
                Name = $"{Common.Names.NavPathBuilder}",
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                Summary = new List<string> { "A path of navigation properties being built, standing on the entity of TCurrent. Then (in NavPropQueryExtensions) exists only when TCurrent has navigation properties." },
                GenericParams = new GenericParameterList
                {
                    _genericNavProp,
                    new GenericParameter { Name = "TCurrent", Constraint = Common.Names.PropertyContainer }
                }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.Line($"private readonly {Common.Names.NavPropsBuilder}<{_genericProp}> _root");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = $"{Common.Names.NavPathBuilder}",
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "root", ParamType = $"{Common.Names.NavPropsBuilder}<{_genericProp}>" },
                },
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_root = root");
            CSharpFile.CloseBlock();

            PropertyExtensions.FullNoSetter(new TypeInfo($"{Common.Names.NavPropsBuilder}<{_genericProp}>"), "Root", AccessModifiers.Internal, backingField: "_root");

            var method = new MethodDeclaration
            {
                Name = "Append",
                AccessModifier = AccessModifiers.Public,
                ReturnType = new TypeInfo($"{Common.Names.NavPathBuilder}<{_genericProp.Name}, TTarget>"),
                ReturnTypeForComments = "NavPathBuilder",
                GenericParams = new GenericParameterList { new GenericParameter { Name = "TTarget", Constraint = Common.Names.PropertyContainer } },
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "prop", ParamType = $"{Common.Names.NavProp}<{_genericProp}, TTarget>", Description = "Navigation Property" } },
                Summary = new List<string> { "Add a Navigation Property to the root level of the query." }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("_root.Append(prop)");
            CSharpFile.CloseBlock();

            method.Name = "Append";
            method.GenericParams = null;
            method.ReturnType = new TypeInfo($"{Common.Names.NavPropsBuilder}<{_genericParams}>");
            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter { Name = "props", ParamType = $"{Common.Names.NavPropCollection}<{_genericParams}>", Description = "Navigation Property Collection" });
            method.Summary = new List<string> { "Add a Navigation Property Collection to the root level of the query." };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("_root.Append(props)");
            CSharpFile.CloseBlock();

            method.Name = "EndNavProps";
            method.Parameters.Clear();
            method.ReturnType = new TypeInfo(_genericQuery);
            method.Summary = new List<string> { "Ends setting the navigation properties." };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("_root.EndNavProps()");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }
    }
}

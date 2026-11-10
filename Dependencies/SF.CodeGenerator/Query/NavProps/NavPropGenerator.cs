#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateNavProp(ModelContainer modelContainer)
    {
        return NavPropClass.Generate(modelContainer);
    }
    private static class NavPropClass
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
            CSharpFile.OpenNamespace(typeNamespace);
            GenerateBaseClass(typeNamespace);
            GenerateContainerClass(typeNamespace);
            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }

        private static void GenerateBaseClass(string typeNamespace)
        {
            var navPropCollection = Common.Names.NavPropCollection;

            var type = new ClassDeclaration
            {
                Name = Common.Names.NavProp,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                Summary = new List<string> { "This class represents a single Navigation Property." }
            };

            CSharpFile.OpenClass(type);
            PropertyExtensions.AutoProtectedSetter(TypeInfo.String, "Name");
            PropertyExtensions.AutoProtectedSetter(TypeInfo.String, "FullName");
            PropertyExtensions.AutoProtectedSetter(TypeInfo.String, "StringValue");
            PropertyExtensions.AutoProtectedSetter(TypeInfo.Int, "MetadataToken");
            PropertyExtensions.AutoProtectedSetter(TypeInfo.Object, "Value");
            PropertyExtensions.AutoProtectedSetter(TypeInfo.Int, "IntValue");
            PropertyExtensions.AutoProtectedSetter(new TypeInfo(navPropCollection), "NavProps", description: "Sub Navigation Properties for this Navigation Property");

            var method = new MethodDeclaration
            {
                Name = QueryMethodNames.AddSubNavProp,
                AccessModifier = AccessModifiers.Internal,
                ReturnType = new TypeInfo(type.Name),
                Parameters = new List<MethodParameter>
                {
                    new MethodParameter { Name = "id", ParamType = "int" },
                    new MethodParameter { Name = "name", ParamType = "string" },
                    new MethodParameter { Name = "owner", ParamType = "Type" }
                }
            };
            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("NavProps == null");
            CSharpFile.LineIndented($"NavProps = new {navPropCollection}()");
            CSharpFile.Line($"var navProp = new {Common.Names.ContainerNavProp}(id, name, owner)");
            CSharpFile.If("NavProps.Contains(navProp)");
            CSharpFile.ReturnIndented("NavProps.Get(navProp)");
            CSharpFile.Line("NavProps.Add(navProp)");
            CSharpFile.Return("navProp");
            CSharpFile.CloseBlock();

            method = new MethodDeclaration
            {
                Name = "ToString",
                ReturnType = TypeInfo.String,
                InheritanceModifier = InheritanceModifiers.Override
            };
            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return("$\"{FullName}.{StringValue}\"");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }

        private static void GenerateContainerClass(string typeNamespace)
        {
            var type = new ClassDeclaration
            {
                Name = Common.Names.ContainerNavProp,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Internal,
                InheritanceModifier = InheritanceModifiers.Sealed,
                BaseClass = Common.Names.NavProp,
                Summary = new List<string> { "A single Navigation Property built from a navigation property container: its id is the nav prop enum as an int." }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.ContainerNavProp,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "id", ParamType = "int" },
                  new MethodParameter { Name = "name", ParamType = "string" },
                  new MethodParameter { Name = "owner", ParamType = "Type" },
                },
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("MetadataToken = owner.MetadataToken");
            CSharpFile.Line("FullName = owner.FullName");
            CSharpFile.Line("Name = owner.Name");
            CSharpFile.Line("StringValue = name");
            CSharpFile.Line("Value = id");
            CSharpFile.Line("IntValue = id");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
        }
    }
}

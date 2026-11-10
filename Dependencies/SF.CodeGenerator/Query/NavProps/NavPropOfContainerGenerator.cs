#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GenerateNavPropOfContainer(ModelContainer modelContainer)
    {
        return NavPropOfContainerClass.Generate(modelContainer);
    }

    private static class NavPropOfContainerClass
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
                Name = Common.Names.NavProp,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                InheritanceModifier = InheritanceModifiers.Sealed,
                GenericParams = new GenericParameterList
                {
                    new GenericParameter { Name = "TOwner", Constraint = Common.Names.NavPropertyContainer },
                    new GenericParameter { Name = "TTarget", Constraint = Common.Names.PropertyContainer }
                },
                Summary = new List<string>
                {
                    "One navigation property of TOwner, leading to the entity of TTarget. Only containers of entities with navigation properties have them, and Then only takes a navigation property of the entity the previous one led to."
                }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.Line("private readonly int _enumValue");
            CSharpFile.Line("private readonly string _name");

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.NavProp,
                AccessModifier = AccessModifiers.Internal,
                Parameters = new List<MethodParameter>(2)
                {
                    new MethodParameter { Name = "enumValue", ParamType = "int" },
                    new MethodParameter { Name = "name", ParamType = "string" }
                }
            });

            CSharpFile.OpenBlock();
            CSharpFile.Line("_enumValue = enumValue");
            CSharpFile.Line("_name = name");
            CSharpFile.CloseBlock();

            PropertyExtensions.FullNoSetter(TypeInfo.Int, "EnumValue", backingField: "_enumValue");
            PropertyExtensions.FullNoSetter(TypeInfo.String, "Name", backingField: "_name");

            var method = new MethodDeclaration
            {
                Name = "CreateItem",
                AccessModifier = AccessModifiers.Internal,
                ReturnType = new TypeInfo(Common.Names.NavProp)
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"new {Common.Names.ContainerNavProp}(_enumValue, _name, typeof(TOwner))");
            CSharpFile.CloseBlock();

            CSharpFile.CloseType();
            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

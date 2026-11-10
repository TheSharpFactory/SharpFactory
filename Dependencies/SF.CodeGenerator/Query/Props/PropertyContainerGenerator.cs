#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class QueryGenerator
{
    public static string GeneratePropertyContainer(ModelContainer modelContainer)
    {
        return PropertyContainerClass.Generate(modelContainer);
    }

    private static class PropertyContainerClass
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

            CSharpFile.Header(ArtifactKind.Class, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(new List<UsingNamespace>(0));
            CSharpFile.OpenNamespace(typeNamespace);

            var type = new ClassDeclaration
            {
                Name = Common.Names.PropertyContainer,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                Summary = new List<string>
                {
                    "Base of the generated property containers. A query is declared on one container and only takes that container's properties."
                }
            };

            CSharpFile.OpenClass(type);
            CSharpFile.CloseType();

            type = new ClassDeclaration
            {
                Name = Common.Names.NavPropertyContainer,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                BaseClass = Common.Names.PropertyContainer,
                Summary = new List<string>
                {
                    "Base of the containers of entities that have navigation properties: only their queries get the navigation property methods."
                }
            };

            CSharpFile.OpenClass(type);
            CSharpFile.CloseType();

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

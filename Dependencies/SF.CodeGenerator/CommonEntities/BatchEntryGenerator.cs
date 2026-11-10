#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class CommonEntityGenerator
{
    public static string GenerateBatchEntry(ModelContainer modelContainer)
    {
        return BatchEntry.Generate(modelContainer);
    }
    private static class BatchEntry
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
            var usingNamespaces = new List<UsingNamespace>(0)
            {
            };

            var type = new ClassDeclaration
            {
                Name = Common.Names.BatchEntry,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string>(1) { "This class represents one entry in a batch of changes." },
                GenericParams = new GenericParameterList { new GenericParameter { Name = "TEntity" } }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            PropertyExtensions.Auto(new TypeInfo("TEntity"), "Original", description: "Original Entity.");
            PropertyExtensions.Auto(new TypeInfo("TEntity"), "Changed", description: "Changed Entity.");

            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

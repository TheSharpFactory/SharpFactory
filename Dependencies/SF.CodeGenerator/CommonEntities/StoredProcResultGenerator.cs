#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class CommonEntityGenerator
{
    public static string GenerateStoredProcResult(ModelContainer modelContainer)
    {
        return StoredProcResult.Generate(modelContainer);
    }
    private static class StoredProcResult
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" }
            };

            var type = new ClassDeclaration
            {
                Name = Common.Names.StoredProcResult,
                Namespace = typeNamespace,
                IsPartial = true,
                Summary = new List<string>(1) { "This class represents the Result of a stored procedure call." },
                GenericParams = new GenericParameterList { new GenericParameter { Name = "TResult" } }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            PropertyExtensions.Auto(new TypeInfo("TResult", isList: true), "Result", description: "Gets or sets the result set.");
            PropertyExtensions.Auto(TypeInfo.Int, "ReturnValue", description: "Gets or sets the Return Value.");

            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}

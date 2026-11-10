#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class CommonEntityGenerator
{
    public static string GenerateExceptionClass(ModelContainer modelContainer)
    {
        return Exception.Generate(modelContainer);
    }
    private static class Exception
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeNamespace = modelContainer.CommonLayers.Exceptions.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(1)
            {
              new UsingNamespace{ Name= "System" }
            };

            var type = new ClassDeclaration
            {
                Name = Common.Names.DataExchangeException,
                Namespace = typeNamespace,
                IsPartial = false,
                AccessModifier = AccessModifiers.Public,
                BaseClass = "ApplicationException",
                Summary = new List<string>(1) { "This exception is raised when the Data Exchange fails." }
            };

            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);
            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.DataExchangeException,
                AccessModifier = AccessModifiers.Public,
                BaseConstructor = "base()",
                Summary = new List<string>(1) { "Default constructor" }
            });
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.DataExchangeException,
                AccessModifier = AccessModifiers.Public,
                BaseConstructor = "base(message)",
                Summary = new List<string>(1) { "Constructor with exception message" },
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "message", ParamType = "string", Description = "Exception Message" }
                }
            });
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            CSharpFile.Constructor(new ConstructorDeclaration
            {
                Name = Common.Names.DataExchangeException,
                AccessModifier = AccessModifiers.Public,
                BaseConstructor = "base(message, inner)",
                Summary = new List<string>(1) { "Constructor with message and inner exception" },
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "message", ParamType = "string", Description = "Exception Message" },
                  new MethodParameter { Name = "inner", ParamType = "Exception", Description = "Inner Exception" }
                }
            });
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();
            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
}

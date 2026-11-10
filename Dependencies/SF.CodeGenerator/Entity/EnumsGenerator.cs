#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static class EnumsGenerator
{
    public static string GenerateEntityPropertyEnum(CommonSettings commonSettings, Model model, Entity entity, Schema schema)
    {
        return EntityPropertyEnum.Generate(commonSettings, model, entity, schema);
    }

    public static string GenerateStoredProcPropertyEnum(CommonSettings commonSettings, Model model, StoredProc proc, Schema schema)
    {
        return StoredProcPropertyEnum.Generate(commonSettings, model, proc, schema);
    }

    public static string GenerateNavPropertyEnum(CommonSettings commonSettings, Model model, Entity entity, Schema schema)
    {
        return NavPropertyEnum.Generate(commonSettings, model, entity, schema);
    }

    private static class EntityPropertyEnum
    {
        public static string Generate(CommonSettings commonSettings, Model model, Entity entity, Schema schema)
        {
            if(!(entity.Ps?.Count > 0))
                return string.Empty;

            CSharpFile.Start(commonSettings.IndentSize);

            var typeNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.PropEnum, model, schema);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new EnumDeclaration
            {
                Name = Common.GeneratePropEnumName(entity),
                IsPartial = false,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                Summary = new List<string>(1) { $"This enum represents the existing Properties for: {entity.N}." }
            };

            //Generate file header with information about the class.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenEnumWithNamespace(type);

            foreach(var p in entity.Ps)
                EnumExtensions.PropEnumMember(p);

            //b.AppendLine("None");
            CSharpFile.CloseType();

            //here create the equality comparer to be used with all dictionaries and hashsets
            //EqualityComparerGenerator.Generate(commonSettings, typeName);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
    private static class StoredProcPropertyEnum
    {
        public static string Generate(CommonSettings commonSettings, Model model, StoredProc proc, Schema schema)
        {
            if(!proc.HPar)
                return string.Empty;

            CSharpFile.Start(commonSettings.IndentSize);

            var typeNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.PropEnum, model, schema);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new EnumDeclaration
            {
                Name = Common.GeneratePropEnumName(proc),
                IsPartial = false,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                Summary = new List<string>(1) { $"This enum represents the existing Parameters for Stored Proc:{proc.N}." }
            };

            //Generate file header with information about the class.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenEnumWithNamespace(type);

            foreach(var p in proc.Par)
                EnumExtensions.PropEnumMember(p);

            //b.AppendLine("None");
            CSharpFile.CloseType();

            //here create the equality comparer to be used with all dictionaries and hashsets
            //EqualityComparerGenerator.Generate(commonSettings, typeName);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
    private static class NavPropertyEnum
    {
        public static string Generate(CommonSettings commonSettings, Model model, Entity entity, Schema schema)
        {
            if(!(entity.NPs?.Count > 0))
                return string.Empty;

            CSharpFile.Start(commonSettings.IndentSize);

            var typeNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.NavPropEnum, model, schema);

            var usingNamespaces = new List<UsingNamespace>(2)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            var type = new EnumDeclaration
            {
                Name = Common.GenerateNavPropEnumName(entity),
                IsPartial = false,
                Namespace = typeNamespace,
                AccessModifier = AccessModifiers.Public,
                Summary = new List<string>(1) { "This enum represents the existing Navigation Properties for a DTO." }
            };

            //Generate file header with information about the class.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenEnumWithNamespace(type);

            //b.AppendLine("All,");

            foreach(var p in entity.NPs)
            {
                var fk = model.FKeys.GetById(p.FKId);
                if(!(fk.FKParts?.Count > 0))
                    continue;

                EnumExtensions.NavPropEnumMember(model, p, fk);
            }

            //b.AppendLine("None");
            CSharpFile.CloseType();

            //here create the equality comparer to be used with all dictionaries and hashsets
            //EqualityComparerGenerator.Generate(commonSettings, typeName);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
    }
}

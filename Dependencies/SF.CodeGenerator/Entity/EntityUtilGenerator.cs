#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static class EntityUtilGenerator
{
    #region Entity
    public static string Generate(ModelContainer modelContainer, Model model, Entity entity, Schema schema)
    {
        var commonSettings = modelContainer.CommonSettings;
        CSharpFile.Start(commonSettings.IndentSize);

        var typeNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.EntityUtil, model, schema);
        var entityNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.Entity, model, schema);
        var entityFullName = Common.GenerateTypeFullName(commonSettings, model.Layers.Entity, model, schema, entity.N);
        var propEnumNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.PropEnum, model, schema);
        var filterNamespace = modelContainer.CommonLayers.Query.FullNamespace(commonSettings);

        var usingNamespaces = new List<UsingNamespace>(3)
  {
    new UsingNamespace{ Name = "System.Collections.Generic" },
    new UsingNamespace{ Name = propEnumNamespace },
    new UsingNamespace{ Name =  filterNamespace}
  };

        var entNamespace = new UsingNamespace { Name = entityNamespace };
        if(string.CompareOrdinal(entityNamespace, typeNamespace) != 0)//if the entity is in a different namespace then add it
        {
            if(entity.FullyQualify)//if the schema name is the same as the entity we need to hack the namespace to avoid a weird visual studio issue
            {
                entNamespace.AliasRequired = true;
                entNamespace.Alias = entity.N + "Namespace";
                entNamespace.AliasComment = "//this is required because the schema's name is the same as the entity's name.";
            }

            usingNamespaces.Add(entNamespace);
        }

        var type = new ClassDeclaration
        {
            Name = Common.GenerateEntityUtilName(entity),
            Namespace = typeNamespace,
            AccessModifier = AccessModifiers.Public,
            IsStatic = true,
            IsPartial = true,
            Summary = new List<string>(1) { $"This class is a utility class for a {entityFullName}." }
        };
        //Generate file header with information about the interface.
        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenClassWithNamespace(type);
        GenerateBody(entity, entNamespace);
        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }
    private static void GenerateBody(Entity entity, UsingNamespace entityNamespace)
    {
        GenerateHasChangesFunction(entity, entityNamespace);
        GenerateMergeFunction(entity, entityNamespace);
        GenerateGetChangesFunction(entity, entityNamespace);
    }
    #endregion

    #region GenerateMethods
    private static void GenerateHasChangesFunction(Entity entity, UsingNamespace entityNamespace)
    {
        var methodInfo = EntityUtilsMethodSignatures.GenerateHasChanges(entity, entityNamespace);
        CSharpFile.OpenBlock();
        CSharpFile.Comment("this method returns true if differences are found between the 2 entities.");
        CSharpFile.OpenRegion("Detect Changes");

        var oneParam = methodInfo.Parameters[0].Name;
        var twoParam = methodInfo.Parameters[1].Name;
        var propCount = entity.Ps.Count;

        for(var i = 0; i < propCount; i++)
        {
            var prop = entity.Ps[i];
            var propName = prop.N;
            var propType = prop.T;
            var isByteArray = string.CompareOrdinal(propType, "byte[]") == 0;
            var isString = prop.Str;
            if(isByteArray)
                CSharpFile.If($"!{Common.Names.Compare}.{EntityUtilsMethodNames.ByteArrayEquals}({oneParam}.{propName}, {twoParam}.{propName})");
            else if(isString)
                CSharpFile.If($"string.CompareOrdinal({oneParam}.{propName}, {twoParam}.{propName}) != 0");
            else
                CSharpFile.If($"{oneParam}.{propName} != {twoParam}.{propName}");
            CSharpFile.ReturnIndented("true");
        }

        CSharpFile.CloseRegion();
        CSharpFile.Return("false");
        CSharpFile.CloseBlock();
    }
    private static void GenerateMergeFunction(Entity entity, UsingNamespace entityNamespace)
    {
        var methodInfo = EntityUtilsMethodSignatures.GenerateMerge(entity, entityNamespace);

        CSharpFile.OpenBlock();
        CSharpFile.Comment("this method merges 2 Entities.");
        CSharpFile.OpenRegion("Merge Values");

        var propCount = entity.Ps.Count;
        var sourceParam = methodInfo.Parameters[0].Name;
        var targetParam = methodInfo.Parameters[1].Name;

        for(var i = 0; i < propCount; i++)
        {
            var propName = entity.Ps[i].N;
            CSharpFile.Line(targetParam + "." + propName + " = " + sourceParam + "." + propName);
        }

        CSharpFile.CloseRegion();
        CSharpFile.CloseBlock();
    }
    private static void GenerateGetChangesFunction(Entity entity, UsingNamespace entityNamespace)
    {
        var containerName = Common.GeneratePropClassName(entity);
        var filterConditionsEnum = Common.Names.FilterConditionsEnum;
        var filterCollection = $"List<{Common.Names.IQueryFilter}>";
        var filterName = Common.Names.QueryFilter;

        var propCount = entity.Ps.Count;
        var methodInfo = EntityUtilsMethodSignatures.GenerateGetChanges(entity, entityNamespace);
        var originalParam = methodInfo.Parameters[0].Name;
        var changedParam = methodInfo.Parameters[1].Name;
        CSharpFile.OpenBlock();
        CSharpFile.Comment("this method returns a list of changes.");
        CSharpFile.Line($"var changes = new {filterCollection}({propCount})");
        CSharpFile.OpenRegion("Detect Changes");
        for(var i = 0; i < propCount; i++)
        {
            var prop = entity.Ps[i];
            var propName = prop.N;
            var propType = prop.T;
            var isByteArray = string.CompareOrdinal(propType, "byte[]") == 0;
            var isString = prop.Str;
            if(isByteArray)
                CSharpFile.If($"!{Common.Names.Compare}.{EntityUtilsMethodNames.ByteArrayEquals}({originalParam}.{propName}, {changedParam}.{propName})");
            else if(isString)
                CSharpFile.If($"string.CompareOrdinal({originalParam}.{propName}, {changedParam}.{propName}) != 0");
            else
                CSharpFile.If($"{originalParam}.{propName} != {changedParam}.{propName}");
            CSharpFile.LineIndented($"changes.Add({filterName}.New({containerName}.{propName}, {filterConditionsEnum}.Equals, {methodInfo.Parameters[1].Name}.{propName}))");
        }

        CSharpFile.CloseRegion();
        CSharpFile.Return("changes.Count > 0 ? changes : null");
        CSharpFile.CloseBlock();
    }

    //private static bool ByteArraysEqual(byte[] a, byte[] b)
    //{
    //if(a == null && b == null)
    //  return true;
    //if(a == null && b != null)
    //  return false;
    //if(a != null && b == null)
    // return false;
    //if(a.Length != b.Length)
    //  return false;     
    //if(object.ReferenceEquals(a, b))
    //  return true; 
    //  var length = a.Length;
    //  for(var i = 0; i < length; i++)      
    //    if(a[i] != b[i])
    //      return false;      
    //  return true;
    //}
    #endregion
}

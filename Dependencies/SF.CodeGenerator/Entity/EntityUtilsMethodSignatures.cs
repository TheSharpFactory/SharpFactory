#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

internal static class EntityUtilsMethodSignatures
{

    public static MethodDeclaration GenerateHasChanges(Entity entity, UsingNamespace entityNamespace, AccessModifiers accesibility = AccessModifiers.Public)
    {
        //var entityFullName = Common.GenerateTypeFullName(model.Layers.Entities, schema, entity.Name);
        var typeName = Common.GenerateTypeName(entity);
        var summary = $"Compare 2 Entities for changes. NavProperties are NOT taken into account. For NavProperties use the corresponding utils for those Entities";

        var returnTypeForComments = "True if there is changes. False if no changes found.";

        var entOneVarName = "one";
        var entTwoVarName = "two";

        var paramType = !entity.FullyQualify ? typeName : $"{entityNamespace.Alias}.{typeName}";

        var methodParams = new List<MethodParameter>(2)
        {
           new MethodParameter { Name = entOneVarName, ParamType = paramType, Description = $"Entity One." },
           new MethodParameter { Name = entTwoVarName, ParamType = paramType, Description = $"Entity Two." },
        };

        var methodInfo = new MethodDeclaration
        {
            Name = EntityUtilsMethodNames.HasChanges,
            AccessModifier = accesibility,
            ReturnType = TypeInfo.Bool,
            Summary = new List<string> { summary },
            ReturnTypeForComments = returnTypeForComments,
            IsInterfaceMember = false,
            IsStatic = true,
            Parameters = methodParams
        };

        CSharpFile.DeclareMethod(methodInfo);
        return methodInfo;
    }
    public static MethodDeclaration GenerateMerge(Entity entity, UsingNamespace entityNamespace, AccessModifiers accesibility = AccessModifiers.Public)
    {
        var typeName = Common.GenerateTypeName(entity);
        var summary = $"Merge all values from one Entity into another one.";

        var returnTypeForComments = "void.";

        var entSourceVarName = "source";
        var entTargetVarName = "target";

        var paramType = !entity.FullyQualify ? typeName : $"{entityNamespace.Alias}.{typeName}";

        var methodParams = new List<MethodParameter>(2)
        {
           new MethodParameter { Name = entSourceVarName, ParamType = paramType, Description = "Source Entity. Will be copied to the target." },
           new MethodParameter { Name = entTargetVarName, ParamType = paramType, Description = "Target Entity. Will receive the values from the source." },
        };

        var methodInfo = new MethodDeclaration
        {
            Name = EntityUtilsMethodNames.Merge,
            AccessModifier = accesibility,
            ReturnType = TypeInfo.Void,
            Summary = new List<string> { summary },
            ReturnTypeForComments = returnTypeForComments,
            IsInterfaceMember = false,
            IsStatic = true,
            Parameters = methodParams
        };

        CSharpFile.DeclareMethod(methodInfo);
        return methodInfo;
    }
    public static MethodDeclaration GenerateGetChanges(Entity entity, UsingNamespace entityNamespace, AccessModifiers accesibility = AccessModifiers.Public)
    {
        //var entityFullName = Common.GenerateTypeFullName(model.Layers.Entities, schema, entity.Name);
        var containerName = Common.GeneratePropClassName(entity);
        var filterCollection = $"List<{Common.Names.IQueryFilter}>";
        var typeName = Common.GenerateTypeName(entity);
        var summary = $"Get a list of {Common.Names.QueryFilter} that represent changed fields.";
        var returnType = filterCollection;
        var returnTypeForComments = "List of " + containerName;

        var entOriginalVarName = "original";
        var entChangedVarName = "changed";

        var paramType = !entity.FullyQualify ? typeName : $"{entityNamespace.Alias}.{typeName}";

        var methodParams = new List<MethodParameter>(2)
        {
           new MethodParameter { Name = entOriginalVarName, ParamType = paramType, Description = "Original Entity. The unchanged entity." },
           new MethodParameter { Name = entChangedVarName, ParamType = paramType, Description = "Changed Entity. The entity holding the changed fields." },
        };

        var methodInfo = new MethodDeclaration
        {
            Name = EntityUtilsMethodNames.GetChanges,
            AccessModifier = accesibility,
            ReturnType = new TypeInfo(returnType),
            Summary = new List<string> { summary },
            ReturnTypeForComments = returnTypeForComments,
            IsInterfaceMember = false,
            IsStatic = true,
            Parameters = methodParams
        };

        CSharpFile.DeclareMethod(methodInfo);
        return methodInfo;
    }
    public static MethodDeclaration GenerateCompareByteArrays(AccessModifiers accesibility = AccessModifiers.Public)
    {
        var methodParams = new List<MethodParameter>(2)
        {
           new MethodParameter { Name = "a", ParamType = "byte[]", Description = "First byte array to compare." },
           new MethodParameter { Name = "b", ParamType = "byte[]", Description = "Second byte array to compare." },
        };

        var methodInfo = new MethodDeclaration
        {
            Name = EntityUtilsMethodNames.ByteArrayEquals,
            AccessModifier = accesibility,
            ReturnType = TypeInfo.Bool,
            Summary = new List<string> { "Compares two byte arrays for equality." },
            ReturnTypeForComments = "Returns true if the arrays are equal.",
            IsInterfaceMember = false,
            IsStatic = true,
            Parameters = methodParams
        };

        CSharpFile.DeclareMethod(methodInfo);
        return methodInfo;
    }
}

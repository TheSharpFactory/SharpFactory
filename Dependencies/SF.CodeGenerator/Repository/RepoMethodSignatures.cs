#region Usings
using System.Collections.Generic;
using System.Globalization;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

internal static class RepoMethodSignatures
{
    internal static class ParamNames
    {
        internal static readonly string _query = "query";
        internal static readonly string _queryFilters = "where";
        internal static readonly string _valuesToSet = "valuesToSet";
        internal static readonly string _sortFilters = "orderby";
        internal static readonly string _paginationSettings = "pagination";
        internal static readonly string _navProps = "navprops";
        internal static readonly string _sqlConnection = "con";
        internal static readonly string _topCount = "topcount";
    }
    /// <summary>
    /// Generate signatures for the Create methods of the repository
    /// </summary>
    internal static class CreateMethods
    {
        public static MethodDeclaration GenerateCreate(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"Creates the specified {entityFullName} in the database.";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if it already exists.";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var entityNameLower = entityName.ToLower();
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = entityNameLower, ParamType = paramType, Description = $"The {entityFullName} to create." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Create}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericCreate(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = "TEntity";
            var summary = $"Creates the specified {entityName} in the database.";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if it already exists.";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.Create}{asyncSuffix}";

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = "entity", ParamType = entityName, Description = $"The {entityName} to create." });

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateCreateList(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"Creates each {entityFullName} from the list in the database.";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.Create}{asyncSuffix}";
            var returnType = isAsync ? "bool" : "void";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var entitySetNameLower = Inflector.Pluralize(entityName).ToLower();
            var methodParams = new List<MethodParameter>(1)
            {
              new MethodParameter{ Name = entitySetNameLower, ParamType = $"List<{paramType}>", Description = $"List of {entityFullName} to create." }
            };

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnType,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericCreateList(bool isAsync, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var summary = $"Creates each {entityName} from the list in the database.";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.Create}{asyncSuffix}";
            var returnType = isAsync ? "bool" : "void";

            var methodParams = new List<MethodParameter>(1)
            {
              new MethodParameter{ Name = "entities", ParamType = $"List<{entityName}>", Description = $"List of {entityName} to create." }
            };

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnType,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
    }
    /// <summary>
    /// Generate signatures for the Retrieve methods of the repository
    /// </summary>
    internal static class RetrieveMethods
    {
        #region Get
        public static MethodDeclaration GenerateGetById(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool hasNavProps, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var summary = $"Gets the specified {entityFullName} from the database by Identity.";
            var returnTypeForComments = $"Single {entityFullName}";
            var methodParams = new List<MethodParameter>(1 + (hasNavProps ? entity.NPs.Count : 0));
            var asyncSuffix = isAsync ? "Async" : "";

            foreach(var p in entity.Ps)
            {
                if(p.Ide == YesOrNo.No)
                    continue;

                var param = p.N.ToLower();
                methodParams.Add(new MethodParameter { Name = param, ParamType = p.T, Description = "The identity of this Entity." });
                break;//if we reach here it means we found it
            }

            summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.GetById}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGetByPK(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool hasNavProps, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var summary = $"Gets the specified {entityFullName} from the database by Primary Key.";
            var returnTypeForComments = $"Single {entityFullName}";
            var methodParams = new List<MethodParameter>(entity.PK.Ps.Count + (hasNavProps ? entity.NPs.Count : 0));
            var asyncSuffix = isAsync ? "Async" : "";

            foreach(var p in entity.PK.Ps)
                methodParams.Add(new MethodParameter { Name = p.N.ToLower(), ParamType = entity.Ps.GetByName(p.N).T, Description = "Primary Key Field." });

            summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.GetByPK}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGetByUK(Entity entity, UsingNamespace entityNamespace, bool isAsync, Key uniqueKey, string entityFullName, string keySuffix, bool isInterfaceMember, bool hasNavProps, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var summary = $"Gets the specified {entityFullName} from the database by Unique Key named {uniqueKey.N}.";
            var returnTypeForComments = $"Single {entityFullName}";
            var methodParams = new List<MethodParameter>(uniqueKey.Ps.Count + (hasNavProps ? entity.NPs.Count : 0));
            var asyncSuffix = isAsync ? "Async" : "";

            foreach(var p in uniqueKey.Ps)
                methodParams.Add(new MethodParameter { Name = p.N.ToLower(), ParamType = entity.Ps.GetByName(p.N).T, Description = "Unique Key Field." });

            summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.GetByUK}{keySuffix}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        #endregion

        #region GetTop
        //internal static MethodDeclaration GenerateGetTop(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isInterfaceMember, bool hasNavProps, bool shareConnection, bool defaultSorting, AccessModifiers accesibility  = AccessModifiers.Public)
        //{
        //  var entityName = entity.N;
        //  var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

        //  var summary = $"Performs a select top N {entityFullName} from the database. Supports filtering.";
        //  var returnTypeForComments = $"List of {entityName}";

        //  var methodParams = new List<MethodParameter>(5);

        //  summary += GenerateSharedConnectionParam(methodParams, shareConnection);

        //  methodParams.Add(new MethodParameter { Name = ParamNames._topCount, ParamType = "int", Description = "Max item count expected in the result." });

        //  summary += GenerateSortFilterListParam(methodParams, entity, defaultSorting);

        //  summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

        //  var methodInfo = new MethodDeclaration
        //  {
        //    Name = RepoMethodNames.GetTop,
        //    AccessModifier = accesibility,
        //    ReturnType = new TypeInfo(returnType, false, false, true),
        //    Summary = new List<string>(1) { summary },
        //    ReturnTypeForComments = returnTypeForComments,
        //    IsInterfaceMember = isInterfaceMember,
        //    IsStatic = false,
        //    Parameters = methodParams
        //  };

        //  CSharpFile.DeclareMethod(methodInfo);
        //  return methodInfo;
        //}
        public static MethodDeclaration GenerateGetTopBasedOnQuery(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
            var summary = $"Performs a select top N {entityFullName} from the database based on a Query object.";
            var returnTypeForComments = $"List of {entityFullName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetTop}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = ParamNames._topCount, ParamType = "int", Description = "Max item count expected in the result." });

            GenerateQueryParam(methodParams, entity);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType, false, false, true),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericGetTopBasedOnQuery(bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = "TEntity";
            var returnType = $"List<{entityName}>";
            var summary = $"Performs a select top N {entityName} from the database based on a Query object.";
            var returnTypeForComments = $"List of {entityName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetTop}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);
            var genericProp = Common.Names.GenericProp;
            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = ParamNames._topCount, ParamType = "int", Description = "Max item count expected in the result." });

            GenerateQueryParam(methodParams, genericProp);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericGetTopNoQuery(Entity entity, bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = entity != null ? entity.N : "TEntity";
            var returnType = $"List<{entityName}>";
            var summary = $"Performs a select top N {entityName} from the database.";
            var returnTypeForComments = $"List of {entityName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetTop}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = ParamNames._topCount, ParamType = "int", Description = "Max item count expected in the result." });

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);

            return methodInfo;
        }
        #endregion

        #region GetFirstOrDefault
        //internal static MethodDeclaration GenerateGetFirstOrDefault(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isInterfaceMember, bool hasNavProps, bool shareConnection, bool defaultSorting, AccessModifiers accesibility  = AccessModifiers.Public)
        //{
        //  var entityName = entity.N;
        //  var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

        //  var summary = $"Performs a select top 1 {entityFullName} from the database. Returns null if none are found.";
        //  var returnTypeForComments = $"Single {entityName}";

        //  var methodParams = new List<MethodParameter>(5);

        //  summary += GenerateSharedConnectionParam(methodParams, shareConnection);

        //  summary += GenerateSortFilterListParam(methodParams, entity, defaultSorting);

        //  summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

        //  var methodInfo = new MethodDeclaration
        //  {
        //    Name = RepoMethodNames.GetFirstOrDefault,
        //    AccessModifier = accesibility,
        //    ReturnType = new TypeInfo(returnType),
        //    Summary = new List<string>(1) { summary },
        //    ReturnTypeForComments = returnTypeForComments,
        //    IsInterfaceMember = isInterfaceMember,
        //    IsStatic = false,
        //    Parameters = methodParams
        //  };

        //  CSharpFile.DeclareMethod(methodInfo);
        //  return methodInfo;
        //}

        public static MethodDeclaration GenerateGetFirstOrDefaultBasedOnQuery(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
            var summary = $"Performs a select top 1 {entityFullName} from the database based on a Query object. Returns null if none are found.";
            var returnTypeForComments = $"Single {entityFullName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetFirstOrDefault}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            GenerateQueryParam(methodParams, entity);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericGetFirstOrDefaultBasedOnQuery(bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var returnType = entityName;
            var summary = $"Performs a select top 1 {entityName} from the database based on a Query object. Returns null if none are found.";
            var returnTypeForComments = $"Single {entityName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetFirstOrDefault}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);
            var genericProp = Common.Names.GenericProp;

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            GenerateQueryParam(methodParams, genericProp);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }

        #endregion

        #region GetFirst
        //internal static MethodDeclaration GenerateGetFirst(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isInterfaceMember, bool hasNavProps, bool shareConnection, bool defaultSorting, AccessModifiers accesibility  = AccessModifiers.Public)
        //{
        //  var entityName = entity.N;
        //  var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

        //  var summary = $"Performs a select top 1 {entityFullName} from the database. Throws InvalidOperationException if none are found.";
        //  var returnTypeForComments = $"Single {entityName}";

        //  var methodParams = new List<MethodParameter>(5);

        //  summary += GenerateSharedConnectionParam(methodParams, shareConnection);

        //  summary += GenerateSortFilterListParam(methodParams, entity, defaultSorting);

        //  summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

        //  var methodInfo = new MethodDeclaration
        //  {
        //    Name = RepoMethodNames.GetFirst,
        //    AccessModifier = accesibility,
        //    ReturnType = new TypeInfo(returnType),
        //    Summary = new List<string>(1) { summary },
        //    ReturnTypeForComments = returnTypeForComments,
        //    IsInterfaceMember = isInterfaceMember,
        //    IsStatic = false,
        //    Parameters = methodParams
        //  };

        //  CSharpFile.DeclareMethod(methodInfo);
        //  return methodInfo;
        //}

        public static MethodDeclaration GenerateGetFirstBasedOnQuery(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
            var summary = $"Performs a select top 1 {entityFullName} from the database based on a Query object. Throws InvalidOperationException if none are found.";
            var returnTypeForComments = $"Single {entityFullName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetFirst}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            GenerateQueryParam(methodParams, entity);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericGetFirstBasedOnQuery(bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var returnType = entityName;
            var summary = $"Performs a select top 1 {entityName} from the database based on a Query object. Throws InvalidOperationException if none are found.";
            var returnTypeForComments = $"Single {entityName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetFirst}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);
            var genericProp = Common.Names.GenericProp;

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            GenerateQueryParam(methodParams, genericProp);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }

        #endregion

        #region GetSingleOrDefault
        //internal static MethodDeclaration GenerateGetSingleOrDefault(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isInterfaceMember, bool hasNavProps, bool shareConnection, bool defaultSorting, AccessModifiers accesibility  = AccessModifiers.Public)
        //{
        //  var entityName = entity.N;
        //  var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

        //  var summary = $"Performs a select top 2 {entityFullName} from the database. Returns null if none are found. Throws InvalidOperationException if more than one is found.";
        //  var returnTypeForComments = $"Single {entityName}";

        //  var methodParams = new List<MethodParameter>(5);

        //  summary += GenerateSharedConnectionParam(methodParams, shareConnection);

        //  summary += GenerateSortFilterListParam(methodParams, entity, defaultSorting);

        //  summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

        //  var methodInfo = new MethodDeclaration
        //  {
        //    Name = RepoMethodNames.GetSingleOrDefault,
        //    AccessModifier = accesibility,
        //    ReturnType = new TypeInfo(returnType),
        //    Summary = new List<string>(1) { summary },
        //    ReturnTypeForComments = returnTypeForComments,
        //    IsInterfaceMember = isInterfaceMember,
        //    IsStatic = false,
        //    Parameters = methodParams
        //  };

        //  CSharpFile.DeclareMethod(methodInfo);
        //  return methodInfo;
        //}

        public static MethodDeclaration GenerateGetSingleOrDefaultBasedOnQuery(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
            var summary = $"Performs a select top 2 {entityFullName} from the database based on a Query object. Returns null if none are found. Throws InvalidOperationException if more than one is found.";
            var returnTypeForComments = $"Single {entityFullName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetSingleOrDefault}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            GenerateQueryParam(methodParams, entity);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericGetSingleOrDefaultBasedOnQuery(bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var returnType = entityName;
            var summary = $"Performs a select top 2 {entityName} from the database based on a Query object. Returns null if none are found. Throws InvalidOperationException if more than one is found.";
            var returnTypeForComments = $"Single {entityName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetSingleOrDefault}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);
            var genericProp = Common.Names.GenericProp;

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            GenerateQueryParam(methodParams, genericProp);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }

        #endregion

        #region GetSingle
        //internal static MethodDeclaration GenerateGetSingle(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isInterfaceMember, bool hasNavProps, bool shareConnection, bool defaultSorting, AccessModifiers accesibility  = AccessModifiers.Public)
        //{
        //  var entityName = entity.N;
        //  var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

        //  var summary = $"Performs a select top 2 {entityFullName} from the database. Throws InvalidOperationException if none are found. Throws InvalidOperationException if more than one is found.";
        //  var returnTypeForComments = $"Single {entityName}";

        //  var methodParams = new List<MethodParameter>(5);

        //  summary += GenerateSharedConnectionParam(methodParams, shareConnection);

        //  summary += GenerateSortFilterListParam(methodParams, entity, defaultSorting);

        //  summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

        //  var methodInfo = new MethodDeclaration
        //  {
        //    Name = RepoMethodNames.GetSingle,
        //    AccessModifier = accesibility,
        //    ReturnType = new TypeInfo(returnType),
        //    Summary = new List<string>(1) { summary },
        //    ReturnTypeForComments = returnTypeForComments,
        //    IsInterfaceMember = isInterfaceMember,
        //    IsStatic = false,
        //    Parameters = methodParams
        //  };

        //  CSharpFile.DeclareMethod(methodInfo);
        //  return methodInfo;
        //}

        public static MethodDeclaration GenerateGetSingleBasedOnQuery(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
            var summary = $"Performs a select top 2 {entityFullName} from the database based on a Query object. Throws InvalidOperationException if none are found. Throws InvalidOperationException if more than one is found.";
            var returnTypeForComments = $"Single {entityFullName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetSingle}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            GenerateQueryParam(methodParams, entity);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericGetSingleBasedOnQuery(bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var returnType = entityName;
            var summary = $"Performs a select top 2 {entityName} from the database based on a Query object. Throws InvalidOperationException if none are found. Throws InvalidOperationException if more than one is found.";
            var returnTypeForComments = $"Single {entityName}";
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.GetSingle}{asyncSuffix}";
            var methodParams = new List<MethodParameter>(2);
            var genericProp = Common.Names.GenericProp;

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            GenerateQueryParam(methodParams, genericProp);

            var methodInfo = new MethodDeclaration
            {
                Name = methodName,
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }

        #endregion

        #region GetList

        public static MethodDeclaration GenerateGetListBasedOnQuery(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
            var returnTypeList = $"List<{returnType}>";

            return Helpers.GenerateGetCollectionBasedOnQuery(entity, returnTypeList, entityFullName, isAsync, isInterfaceMember, hasNavProps, shareConnection, accesibility);
        }
        public static MethodDeclaration GenerateGenericGetListBasedOnQuery(bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var returnType = $"List<TEntity>";
            return Helpers.GenerateGenericGetCollectionBasedOnQuery(isAsync, returnType, isInterfaceMember, hasNavProps, shareConnection, accesibility, skipAwait);
        }
        #endregion

        #region GetAll
        public static MethodDeclaration GenerateGetAll(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool hasNavProps, bool shareConnection, bool defaultSorting, bool supportsPagination, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = entity.N;
            var returnType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
            var returnTypeList = $"List<{returnType}>";

            return Helpers.GenerateGetAllCollection(isAsync, returnTypeList, entity, entityFullName, isInterfaceMember, hasNavProps, shareConnection, defaultSorting, supportsPagination, accesibility);
        }
        public static MethodDeclaration GenerateGenericGetAll(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var returnType = $"List<TEntity>";
            return Helpers.GenerateGenericGetAllCollection(isAsync, returnType, isInterfaceMember, shareConnection, accesibility, skipAwait);
        }
        #endregion

        private static class Helpers
        {
            #region GetCollectioneHelper
            //internal static MethodDeclaration GenerateGetCollection(string returnType, Entity entity, string entityFullName, bool isInterfaceMember, bool hasNavProps, bool shareConnection, bool defaultSorting, bool supportsPagination, AccessModifiers accesibility)
            //{
            //  var summary = $"Gets List of {entityFullName} from the database. Supports filtering.";
            //  var returnTypeForComments = $"List of {entityFullName}";
            //  var methodParams = new List<MethodParameter>(5);

            //  summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            //  summary += GenerateSortFilterListParam(methodParams, entity, defaultSorting);

            //  summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

            //  summary += GeneratePagingParam(methodParams, supportsPagination);

            //  var methodInfo = new MethodDeclaration
            //  {
            //    Name = RepoMethodNames.GetList,
            //    AccessModifier = accesibility,
            //    ReturnType = new TypeInfo(returnType),
            //    Summary = new List<string>(1) { summary },
            //    ReturnTypeForComments = returnTypeForComments,
            //    IsInterfaceMember = isInterfaceMember,
            //    IsStatic = false,
            //    Parameters = methodParams
            //  };

            //  CSharpFile.DeclareMethod(methodInfo);
            //  return methodInfo;
            //}
            public static MethodDeclaration GenerateGetCollectionBasedOnQuery(Entity entity, string returnType, string entityFullName, bool isAsync, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility)
            {
                var summary = $"Gets List of {entityFullName} from the database based on a Query object.";
                var returnTypeForComments = $"List of {entityFullName}";
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.GetList}{asyncSuffix}";
                var methodParams = new List<MethodParameter>(2);

                summary += GenerateSharedConnectionParam(methodParams, shareConnection);

                GenerateQueryParam(methodParams, entity);

                var methodInfo = new MethodDeclaration
                {
                    Name = methodName,
                    AccessModifier = accesibility,
                    ReturnType = new TypeInfo(returnType),
                    Summary = new List<string>(1) { summary },
                    ReturnTypeForComments = returnTypeForComments,
                    IsInterfaceMember = isInterfaceMember,
                    IsStatic = false,
                    IsAsync = isAsync,
                    Parameters = methodParams
                };

                CSharpFile.DeclareMethod(methodInfo);
                return methodInfo;
            }
            public static MethodDeclaration GenerateGenericGetCollectionBasedOnQuery(bool isAsync, string returnType, bool isInterfaceMember, bool hasNavProps, bool shareConnection, AccessModifiers accesibility, bool skipAwait = false)
            {
                var entityName = "TEntity";
                var summary = $"Gets List of {entityName} from the database based on a Query object.";
                var returnTypeForComments = $"List of {entityName}";
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.GetList}{asyncSuffix}";
                var methodParams = new List<MethodParameter>(2);
                var genericProp = Common.Names.GenericProp;

                summary += GenerateSharedConnectionParam(methodParams, shareConnection);

                GenerateQueryParam(methodParams, genericProp);

                var methodInfo = new MethodDeclaration
                {
                    Name = methodName,
                    AccessModifier = accesibility,
                    ReturnType = new TypeInfo(returnType),
                    Summary = new List<string>(1) { summary },
                    ReturnTypeForComments = returnTypeForComments,
                    IsInterfaceMember = isInterfaceMember,
                    IsStatic = false,
                    IsAsync = isAsync,
                    SkipAwait = skipAwait,
                    Parameters = methodParams
                };

                CSharpFile.DeclareMethod(methodInfo);
                return methodInfo;
            }
            #endregion
            #region GetAllCollectioneHelper
            public static MethodDeclaration GenerateGetAllCollection(bool isAsync, string returnType, Entity entity, string entityFullName, bool isInterfaceMember, bool hasNavProps, bool shareConnection, bool defaultSorting, bool supportsPagination, AccessModifiers accesibility)
            {
                var summary = $"Gets List of {entityFullName} from the database. No filtering.";
                var returnTypeForComments = $"List of {entityFullName}";
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.GetList}{asyncSuffix}";

                var methodParams = new List<MethodParameter>(5);

                summary += GenerateSharedConnectionParam(methodParams, shareConnection);

                summary += GenerateSortFilterListParam(methodParams, entity, defaultSorting);

                summary += GenerateNavPropParam(methodParams, entity, hasNavProps);

                summary += GeneratePagingParam(methodParams, supportsPagination);

                var methodInfo = new MethodDeclaration
                {
                    Name = methodName,
                    AccessModifier = accesibility,
                    ReturnType = new TypeInfo(returnType),
                    Summary = new List<string>(1) { summary },
                    ReturnTypeForComments = returnTypeForComments,
                    IsInterfaceMember = isInterfaceMember,
                    IsStatic = false,
                    IsAsync = isAsync,
                    Parameters = methodParams
                };

                CSharpFile.DeclareMethod(methodInfo);
                return methodInfo;
            }
            public static MethodDeclaration GenerateGenericGetAllCollection(bool isAsync, string returnType, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility, bool skipAwait = false)
            {
                var entityName = "TEntity";
                var summary = $"Gets List of {entityName} from the database. No filtering.";
                var returnTypeForComments = $"List of {entityName}";
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.GetList}{asyncSuffix}";

                var methodParams = new List<MethodParameter>(5);

                summary += GenerateSharedConnectionParam(methodParams, shareConnection);

                var methodInfo = new MethodDeclaration
                {
                    Name = methodName,
                    AccessModifier = accesibility,
                    ReturnType = new TypeInfo(returnType),
                    Summary = new List<string>(1) { summary },
                    ReturnTypeForComments = returnTypeForComments,
                    IsInterfaceMember = isInterfaceMember,
                    IsStatic = false,
                    IsAsync = isAsync,
                    SkipAwait = skipAwait,
                    Parameters = methodParams
                };

                CSharpFile.DeclareMethod(methodInfo);
                return methodInfo;
            }
            #endregion
        }
    }
    /// <summary>
    /// Generate signatures for the Update methods of the repository
    /// </summary>
    internal static class UpdateMethods
    {
        public static MethodDeclaration GenerateUpdate(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"<para>Updates the specified {entityFullName} in the database.</para><para>This method only updates those fields that have changed.</para><para>If no fields have changed then no call is made to the database.</para>";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if it does not exist or if no changes were made.";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var entityNameLower = entityName.ToLower();
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = entityNameLower, ParamType = paramType, Description = $"The {entityFullName} to update." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericUpdate(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = "TEntity";
            var summary = $"<para>Updates the specified {entityName} in the database.</para><para>This method only updates those fields that have changed.</para><para>If no fields have changed then no call is made to the database.</para>";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if it does not exist or if no changes were made.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = "entity", ParamType = entityName, Description = $"The {entityName} to update." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateUpdateOptimistic(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"<para>Updates the specified {entityFullName} in the database.</para><para>This method only updates those fields that have changed. Compares original to changed.</para><para>If no fields have changed then no call is made to the database.</para><para>WARNING: This method allows changes to PrimaryKey, Unique Keys, and Identity.</para>";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if no changes found.";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var origVarName = "original";
            var changedVarName = "changed";

            var methodParams = new List<MethodParameter>(3);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = origVarName, ParamType = paramType, Description = "The original Entity." });
            methodParams.Add(new MethodParameter { Name = changedVarName, ParamType = paramType, Description = "The changed Entity." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateUpdateOptimisticBatch(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"<para>Updates a batch of {entityFullName} in the database.</para><para>This method only updates those fields that have changed. Compares original to changed.</para><para>If no fields have changed then no call is made to the database.</para><para>WARNING: This method allows changes to PrimaryKey, Unique Keys, and Identity.</para>";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if no changes found.";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            paramType = $"List<BatchEntry<{paramType}>>";

            var batchEntries = "batchEntries";

            var methodParams = new List<MethodParameter>(3);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = batchEntries, ParamType = paramType, Description = "Batch of entities." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericUpdateOptimistic(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = "TEntity";
            var summary = $"<para>Updates the specified {entityName} in the database.</para><para>This method only updates those fields that have changed. Compares original to changed.</para><para>If no fields have changed then no call is made to the database.</para><para>WARNING: This method allows changes to PrimaryKey, Unique Keys, and Identity.</para>";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if no changes found.";
            var asyncSuffix = isAsync ? "Async" : "";

            var origVarName = "original";
            var changedVarName = "changed";

            var methodParams = new List<MethodParameter>(3);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = origVarName, ParamType = entityName, Description = "The original Entity." });
            methodParams.Add(new MethodParameter { Name = changedVarName, ParamType = entityName, Description = "The changed Entity." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericUpdateOptimisticBatch(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = "TEntity";
            var paramType = $"List<BatchEntry<{entityName}>>";
            var summary = $"<para>Updates a batch of {entityName} in the database.</para><para>This method only updates those fields that have changed. Compares original to changed.</para><para>If no fields have changed then no call is made to the database.</para><para>WARNING: This method allows changes to PrimaryKey, Unique Keys, and Identity.</para>";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if no changes found.";
            var asyncSuffix = isAsync ? "Async" : "";

            var batchEntries = "batchEntries";

            var methodParams = new List<MethodParameter>(3);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = batchEntries, ParamType = paramType, Description = "Batch of entities." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateUpdateOrCreate(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"<para>Updates the specified {entityFullName} in the database. If not found then it is created.</para><para>This function only updates those fields that have changed.</para><para>If no fields have changed then no call is made to the database.</para>";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if it already exists but no chages were made.";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var entityNameLower = entityName.ToLower();
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = entityNameLower, ParamType = paramType, Description = $"The {entityFullName} to update or create." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.UpdateOrCreate}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericUpdateOrCreate(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = "TEntity";
            var summary = $"<para>Updates the specified {entityName} in the database. If not found then it is created.</para><para>This function only updates those fields that have changed.</para><para>If no fields have changed then no call is made to the database.</para>";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if it already exists but no chages were made.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = "entity", ParamType = entityName, Description = $"The {entityName} to update or create." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.UpdateOrCreate}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateUpdateList(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"<para>Updates each {entityFullName} from the list in the database.</para><para>This method only updates those fields that have changed.</para><para>If no fields have changed then no call is made to the database.</para>";
            var returnType = isAsync ? "bool" : "void";
            var returnTypeForComments = isAsync ? "bool" : "void";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var entitySetNameLower = Inflector.Pluralize(entityName).ToLower();
            var methodParams = new List<MethodParameter>(1)
            {
              new MethodParameter { Name = entitySetNameLower, ParamType = $"List<{paramType}>", Description = $"List of {entityFullName} to update." }
            };

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericUpdateList(bool isAsync, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var summary = $"<para>Updates each {entityName} from the list in the database.</para><para>This method only updates those fields that have changed.</para><para>If no fields have changed then no call is made to the database.</para>";
            var returnType = isAsync ? "bool" : "void";
            var returnTypeForComments = isAsync ? "bool" : "void";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(1)
            {
              new MethodParameter { Name = "entities", ParamType=$"List<{entityName}>", Description = $"List of {entityName} to update." }
            };
            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateUpdateOrCreateList(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"<para>Updates each {entityFullName} from the list in the database. Creates them if not found.</para><para>This method only updates those fields that have changed.</para><para>If no fields have changed then no call is made to the database.</para>";
            var returnType = isAsync ? "bool" : "void";
            var returnTypeForComments = isAsync ? "bool" : "void";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var entitySetNameLower = Inflector.Pluralize(entityName).ToLower();
            var methodParams = new List<MethodParameter>(1)
            {
              new MethodParameter { Name = entitySetNameLower,ParamType=$"List<{paramType}>", Description = $"List of {entityFullName} to update or create." }
            };

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.UpdateOrCreate}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericUpdateOrCreateList(bool isAsync, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var summary = $"<para>Updates each {entityName} from the list in the database. Creates them if not found.</para><para>This method only updates those fields that have changed.</para><para>If no fields have changed then no call is made to the database.</para>";
            var returnType = isAsync ? "bool" : "void";
            var returnTypeForComments = isAsync ? "bool" : "void";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(1)
            {
              new MethodParameter { Name = "entities", ParamType=$"List<{entityName}>", Description = $"List of {entityName} to update or create." }
            };

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.UpdateOrCreate}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateUpdateAnyBasedOnQuery(Entity entity, UsingNamespace entityNamespace, bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = "Updates all Entities from the database that match the query specified.";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var returnType = $"List<{paramType}>";
            var returnTypeForComments = "The list of updated entities.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            summary += GenerateQueryParam(methodParams, entity);

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericUpdateAnyBasedOnQuery(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var entityName = "TEntity";
            var summary = "Updates all Entities from the database that match the query specified.";
            var returnType = $"List<{entityName}>";
            var returnTypeForComments = "The list of updated entities.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);
            var genericProp = Common.Names.GenericProp;

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            summary += GenerateQueryParam(methodParams, genericProp);

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Update}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
    }
    /// <summary>
    /// Generate signatures for the Delete methods of the repository
    /// </summary>
    internal static class DeleteMethods
    {
        public static MethodDeclaration GenerateDeleteById(Entity entity, bool isAsync, string entityFullName, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var returnType = "bool";
            var summary = $"Deletes the specified {entityFullName} from the database by Identity.";
            var returnTypeForComments = "True if succeeded. False if it does not exist.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(1);

            foreach(var p in entity.Ps)
            {
                if(p.Ide == YesOrNo.No)
                    continue;

                var param = p.N.ToLower(CultureInfo.InvariantCulture);

                methodParams.Add(new MethodParameter { Name = param, ParamType = p.T, Description = "The identity of this Entity." });

                break;//if we reach here it means we found it
            }

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.DeleteById}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateDeleteByPK(Entity entity, bool isAsync, string entityFullName, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var returnType = "bool";
            var summary = $"Deletes the specified {entityFullName} from the database by Primary Key.";
            var returnTypeForComments = "True if succeeded. False if it does not exist.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(entity.PK.Ps.Count);

            foreach(var p in entity.PK.Ps)
                methodParams.Add(new MethodParameter { Name = p.N.ToLower(), ParamType = entity.Ps.GetByName(p.N).T, Description = "Primary Key Field." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.DeleteByPK}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateDeleteByUK(Entity entity, Key uniqueKey, bool isAsync, string entityFullName, string keySuffix, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var returnType = "bool";
            var summary = $"Deletes the specified {entityFullName} from the database by Unique Key named {uniqueKey.N}";
            var returnTypeForComments = "True if succeeded. False if it does not exist.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(uniqueKey.Ps.Count);

            foreach(var p in uniqueKey.Ps)
                methodParams.Add(new MethodParameter { Name = p.N.ToLower(), ParamType = entity.Ps.GetByName(p.N).T, Description = "Unique Key Field." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.DeleteByUK}{keySuffix}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateDeleteByEntity(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"Deletes the specified {entityFullName} from the database.";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if it does not exist.";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var entityNameLower = entityName.ToLower();
            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = entityNameLower, ParamType = paramType, Description = $"The {entityFullName} to update." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Delete}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericDeleteByEntity(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var summary = $"Deletes the specified {entityName} from the database.";
            var returnType = "bool";
            var returnTypeForComments = "True if succeeded. False if it does not exist.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            methodParams.Add(new MethodParameter { Name = "entity", ParamType = entityName, Description = $"The {entityName} to update." });

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Delete}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateDeleteAnyBasedOnQuery(Entity entity, bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = "Deletes all Entities from the database that match the filters specified in the Query.";
            var returnType = "int";
            var returnTypeForComments = "The number of deleted records.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            summary += GenerateQueryParam(methodParams, entity);

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Delete}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericDeleteAnyBasedOnQuery(bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var summary = "Deletes all Entities from the database that match the filters specified in the Query.";
            var returnType = "int";
            var returnTypeForComments = "The number of deleted records.";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);
            var genericProp = Common.Names.GenericProp;

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            summary += GenerateQueryParam(methodParams, genericProp);

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Delete}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateDeleteList(Entity entity, UsingNamespace entityNamespace, bool isAsync, string entityFullName, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"Deletes each {entityFullName} in the list from the database.";
            var returnType = isAsync ? "bool" : "void";
            var returnTypeForComments = isAsync ? "bool" : "void";
            var asyncSuffix = isAsync ? "Async" : "";

            var entityName = entity.N;
            var paramType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

            var entitySetNameLower = Inflector.Pluralize(entityName).ToLower();
            var methodParams = new List<MethodParameter>(1)
            {
              new MethodParameter { Name = entitySetNameLower, ParamType=$"List<{paramType}>", Description = $"List of {entityFullName} to delete." }
            };

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Delete}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
        public static MethodDeclaration GenerateGenericDeleteList(bool isAsync, bool isInterfaceMember, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var entityName = "TEntity";
            var summary = $"Deletes each {entityName} in the list from the database.";
            var returnType = isAsync ? "bool" : "void";
            var returnTypeForComments = isAsync ? "bool" : "void";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(1)
            {
              new MethodParameter { Name = "entities", ParamType = $"List<{entityName}>", Description = $"List of {entityName} to delete." }
            };

            var methodInfo = new MethodDeclaration
            {
                Name = $"{RepoMethodNames.Delete}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
    }
    #region Helpers

    private static string GenerateNavPropParam(List<MethodParameter> methodParams, Entity entity, bool hasNavProps)
    {//this assumes that the container has a reference to the correct namespace
        if(!hasNavProps)
            return string.Empty;

        methodParams.Add(new MethodParameter { Name = ParamNames._navProps, ParamType = $"{Common.Names.NavPropCollection}<{Common.GeneratePropClassName(entity)}>", Description = "The navigation properties to include in the result." });
        return " Supports navigation properties.";
    }
    private static string GenerateSharedConnectionParam(List<MethodParameter> methodParams, bool shareConnection)
    {//this assumes that the container has a reference to the correct namespace
        if(!shareConnection)
            return string.Empty;

        methodParams.Add(new MethodParameter { Name = ParamNames._sqlConnection, ParamType = "SqlConnection", Description = "Connection to be shared by multiple calls. The caller is reponsible for closing the connection." });
        return " Supports a shared connection.";
    }
    private static string GenerateQueryParam(List<MethodParameter> methodParams, Entity entity)
    {//this assumes that the container has a reference to the correct namespace     
        var type = $"{Common.Names.Query}<{Common.GeneratePropClassName(entity)}>";
        methodParams.Add(new MethodParameter { Name = ParamNames._query, ParamType = type, Description = "Represents the query to use." });
        return " Supports filtering.";
    }
    private static string GenerateQueryParam(List<MethodParameter> methodParams, string propClass)
    {//this assumes that the container has a reference to the correct namespace     
        var type = $"{Common.Names.Query}<{propClass}>";
        methodParams.Add(new MethodParameter { Name = ParamNames._query, ParamType = type, Description = "Represents the query to use." });
        return " Supports filtering.";
    }
    private static string GenerateSortFilterListParam(List<MethodParameter> methodParams, Entity entity, bool defaultSorting)
    {//this assumes that the container has a reference to the correct namespace
        if(defaultSorting)
            return string.Empty;

        methodParams.Add(new MethodParameter { Name = ParamNames._sortFilters, ParamType = $"List<{Common.Names.ISortFilter}>", Description = "These represent the sorting criteria to be appplied." });
        return " Supports sorting.";
    }
    private static string GeneratePagingParam(List<MethodParameter> methodParams, bool supportsPagination)
    {//this assumes that the container has a reference to the correct namespace
        if(!supportsPagination)
            return string.Empty;

        methodParams.Add(new MethodParameter { Name = ParamNames._paginationSettings, ParamType = Common.Names.PaginationSettings, Description = "Defines the pagination criteria to use. If null no pagination will be used." });
        return " Supports pagination.";
    }
    #endregion

    internal static class StoredProcs
    {
        public static MethodDeclaration Generate(StoredProc proc, bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"Executes Stored Procedure named {proc.N}.";
            var returnType = proc.REn ? $"{Common.Names.StoredProcResult}<{proc.En.N}>" : "int";
            var returnTypeForComments = proc.REn ? $"{Common.Names.StoredProcResult} of {proc.En.N}" : "int";
            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            if(proc.HPar)
            {
                var requestType = Common.GenerateRequestTypeName(proc.N);
                methodParams.Add(new MethodParameter { Name = "request", ParamType = requestType, Description = $"The request containing all parameters to call this stored procedure." });
            }

            var methodInfo = new MethodDeclaration
            {
                Name = $"{proc.N}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
    }
    internal static class Sequences
    {
        public static MethodDeclaration Generate(Sequence seq, bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public, bool skipAwait = false)
        {
            var summary = $"Executes Sequence named {seq.N}.";
            var returnType = seq.Sc.T;
            var returnTypeForComments = seq.Sc.T;

            var asyncSuffix = isAsync ? "Async" : "";

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            var methodInfo = new MethodDeclaration
            {
                Name = $"{seq.N}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                SkipAwait = skipAwait,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);
            return methodInfo;
        }
    }
    internal static class Functions
    {
        public static MethodDeclaration Generate(Function func, bool isAsync, bool isInterfaceMember, bool shareConnection, AccessModifiers accesibility = AccessModifiers.Public)
        {
            var summary = $"Executes Function named {func.N}.";

            var returnType = (string)null;
            var returnTypeForComments = (string)null;

            var asyncSuffix = isAsync ? "Async" : "";

            if(func.REn)
            {
                returnType = $"List<{func.En.N}>";
                returnTypeForComments = $"List of {func.En.N}";
            }
            else
            {
                if(!func.Sc.Str)
                {
                    returnType = func.Sc.T + "?";
                    returnTypeForComments = $"Nullable {func.Sc.T}";
                }
                else
                {
                    returnType = func.Sc.T;
                    returnTypeForComments = func.Sc.T;
                }
            }

            var methodParams = new List<MethodParameter>(2);

            summary += GenerateSharedConnectionParam(methodParams, shareConnection);

            if(func.HPar)
            {
                var requestType = Common.GenerateRequestTypeName(func.N);
                methodParams.Add(new MethodParameter { Name = "request", ParamType = requestType, Description = "The request containing all parameters to call this function." });
            }

            var methodInfo = new MethodDeclaration
            {
                Name = $"{func.N}{asyncSuffix}",
                AccessModifier = accesibility,
                ReturnType = new TypeInfo(returnType),
                Summary = new List<string>(1) { summary },
                ReturnTypeForComments = returnTypeForComments,
                IsInterfaceMember = isInterfaceMember,
                IsStatic = false,
                IsAsync = isAsync,
                Parameters = methodParams
            };

            CSharpFile.DeclareMethod(methodInfo);

            return methodInfo;
        }
    }
}

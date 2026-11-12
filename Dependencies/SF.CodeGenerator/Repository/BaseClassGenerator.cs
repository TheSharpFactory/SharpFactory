#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    public static string GenerateBaseClass(ModelContainer modelContainer)
    {
        return BaseClass.Generate(modelContainer);
    }
    private static class BaseClass
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var commonSettings = modelContainer.CommonSettings;
            var typeName = Common.Names.RepositoryBase;
            var typeNamespace = modelContainer.CommonLayers.Repository.FullNamespace(commonSettings);
            var commonNamespace = modelContainer.CommonLayers.Query.FullNamespace(commonSettings);

            var sqlClientNamespace = Common.CalculateSqlClientNamespace(modelContainer);

            var usingNamespaces = new List<UsingNamespace>(5)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Collections.Generic" },
              new UsingNamespace { Name = "System.Threading.Tasks" },
              new UsingNamespace { Name = sqlClientNamespace },
              new UsingNamespace { Name = "System.Xml.Linq" },
              new UsingNamespace { Name = "System.Threading.Tasks" },
              new UsingNamespace { Name = commonNamespace }
            };

            var summary = "This class is the base class for all Repository classes.";
            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                IsPartial = true,
                Summary = new List<string>(1) { summary }
            };
            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            CSharpFile.OpenRegion("Protected Properties");
            PropertyExtensions.Auto(TypeInfo.Int, ModelIdField, AccessModifiers.Protected);
            PropertyExtensions.Auto(TypeInfo.Int, EntityIdField, AccessModifiers.Protected);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Abstract Methods");

            var method = new MethodDeclaration
            {
                Name = RepoMethodNames.MaterializeEntity,
                ReturnType = TypeInfo.Object,
                AccessModifier = AccessModifiers.Protected,
                InheritanceModifier = InheritanceModifiers.Abstract,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "r", ParamType = "SqlDataReader" },
                  new MethodParameter { Name = "listCapacity", ParamType = "int" }
                }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("List Capacity");
            Materializers.GenerateListCapacity();
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Materialization");

            Materializers.GenerateMaterializeResult();

            Materializers.GenerateMaterializeEntityResult();

            Materializers.GenerateUnboxEntityList();

            Materializers.GenerateAddEntityToList();

            Materializers.GenerateMaterializeXmlProperty();

            CSharpFile.CloseRegion();
            CSharpFile.CloseClass();

            GenerateReadableNoNavProps(typeNamespace);
            GenerateReadableWithNavProps(typeNamespace);

            GenerateWritable(typeNamespace, false);
            GenerateWritable(typeNamespace, true);

            StoredProcBaseClass.Generate(typeNamespace);

            FuncBaseClass.Generate(typeNamespace);

            SeqBaseClass.Generate(typeNamespace);

            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }

        private static void GenerateReadableNoNavProps(string typeNamespace)
        {
            var baseClass = Common.Names.RepositoryBase;
            var typeName = Common.Names.ReadableRepositoryBase;
            var genericProperty = Common.Names.GenericProp;
            var genericEntity = Common.Names.GenericEntity;
            var filterCollection = $"List<{Common.Names.IQueryFilter}>";
            var summary = "This class is the base class for all Repository classes with read capability. No Navigation Property support.";
            //var typeDeclaration = $"public abstract partial class {typeName}<{genericEntity}, {genericProperty}>: {baseClass} where {genericProperty} : struct, IComparable, IFormattable, IConvertible";

            //var summary = $"This class is the base class for all Repository classes.";
            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                BaseClass = baseClass,
                GenericParams = new GenericParameterList
                {
                  new GenericParameter { Name = genericEntity },
                  QueryGenerator._genericProp
                },
                IsPartial = true,
                Summary = new List<string>(1) { summary }
            };

            CSharpFile.OpenClass(type);
            //generate properties
            CSharpFile.OpenRegion("Protected Properties");
            PropertyExtensions.Auto(new TypeInfo($"List<{Common.Names.ISortFilter}>"), DefaultSortField, AccessModifiers.Protected);
            PropertyExtensions.Auto(TypeInfo.Bool, HasTriggersField, AccessModifiers.Protected);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Abstract Methods");

            var method = new MethodDeclaration
            {
                Name = RepoMethodNames.ComposeKeys,
                AccessModifier = AccessModifiers.Protected,
                InheritanceModifier = InheritanceModifiers.Abstract,
                ReturnType = new TypeInfo(filterCollection),
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "entity", ParamType = genericEntity } }
            };

            CSharpFile.DeclareMethod(method);

            method.Name = RepoMethodNames.ComposeInsertPredicate;

            CSharpFile.DeclareMethod(method);

            method.Name = RepoMethodNames.GetChanges;
            method.Parameters.Clear();
            method.Parameters = new List<MethodParameter>
            {
              new MethodParameter { Name = "original", ParamType = genericEntity },
              new MethodParameter { Name = "changed", ParamType = genericEntity },
            };

            CSharpFile.DeclareMethod(method);

            method.Name = RepoMethodNames.Merge;
            method.ReturnType = TypeInfo.Void;
            method.Parameters.Clear();
            method.Parameters = new List<MethodParameter>
            {
              new MethodParameter { Name = "source", ParamType = genericEntity },
              new MethodParameter { Name = "target", ParamType = genericEntity },
            };

            CSharpFile.DeclareMethod(method);

            CSharpFile.CloseRegion();

            GetTopMethods.GenerateNoNavProps();
            GetFirstOrDefaultMethods.GenerateNoNavProps();
            GetFirstMethods.GenerateNoNavProps();
            GetSingleOrDefaultMethods.GenerateNoNavProps();
            GetSingleMethods.GenerateNoNavProps();
            GetListMethods.GenerateGetListNoNavProps();

            CSharpFile.OpenRegion("Materialization");
            Materializers.GenerateBuildEntityTreeNoNavProps();
            CSharpFile.CloseRegion();
            CSharpFile.OpenRegion("Store Queries");
            StoreQueries.GenerateSelectSingle(false, false);
            StoreQueries.GenerateSelectSingle(true, false);
            StoreQueries.GenerateSelectManyNoNavProps(false);
            StoreQueries.GenerateSelectManyNoNavProps(true);
            StoreQueries.GenerateSelectQuery(false);
            StoreQueries.GenerateSelectQuery(true);
            CSharpFile.CloseRegion();
            CSharpFile.CloseType();
        }
        private static void GenerateReadableWithNavProps(string typeNamespace)
        {
            var baseClass = Common.Names.ReadableRepositoryBase;
            var typeName = Common.Names.ReadableNavRepositoryBase;

            var navPropCollection = Common.Names.NavPropCollection;
            //var genericProp = Common.Names.GenericProp();
            var genericEntity = Common.Names.GenericEntity;
            var summary = "This class is the base class for all Repository classes with read capability. With Nav Property support.";

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                BaseClass = $"{baseClass}<{genericEntity}, {Common.Names.GenericProp}>",
                GenericParams = new GenericParameterList { new GenericParameter { Name = genericEntity }, QueryGenerator._genericNavProp },
                IsPartial = true,
                Summary = new List<string>(1) { summary }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.OpenRegion("Abstract Methods");

            var method = new MethodDeclaration
            {
                Name = RepoMethodNames.BuildNavPropInfoTree,
                ReturnType = new TypeInfo($"List<{Common.Names.RepositoryNavPropertyInfo}>"),
                AccessModifier = AccessModifiers.Protected,
                InheritanceModifier = InheritanceModifiers.Abstract,
                Parameters = new List<MethodParameter> { new MethodParameter { Name = "navprops", ParamType = navPropCollection } }
            };

            CSharpFile.DeclareMethod(method);

            method.Name = RepoMethodNames.GetNavPropMaterializers;
            method.ReturnType = TypeInfo.Void;
            method.Parameters = new List<MethodParameter>
            {
              new MethodParameter { Name = "navprops", ParamType = navPropCollection },
              new MethodParameter { Name = "materializers", ParamType = "Func<SqlDataReader, int, object>[]" },
              new MethodParameter { Name = "toOneParents", ParamType = "int[]" }
            };

            CSharpFile.DeclareMethod(method);

            method.Name = RepoMethodNames.BuildObjectGraph;
            method.ReturnType = TypeInfo.Void;
            method.Parameters = new List<MethodParameter>
            {
              new MethodParameter { Name = "entities", ParamType = $"List<{genericEntity}>" },
              new MethodParameter { Name = "results", ParamType = "object[]" },
              new MethodParameter { Name = "navprops", ParamType = navPropCollection }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Materialization");
            Materializers.GenerateBuildEntityTreeWithNavProps();
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Query");
            StoreQueries.GenerateSelectQueryOverride(false);
            StoreQueries.GenerateSelectQueryOverride(true);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Store Queries");
            StoreQueries.GenerateSelectSingle(false, true);
            StoreQueries.GenerateSelectSingle(true, true);
            StoreQueries.GenerateSelectSingleWithContainerNavProps(false);
            StoreQueries.GenerateSelectSingleWithContainerNavProps(true);
            StoreQueries.GenerateSelectManyWithNavProps(false);
            StoreQueries.GenerateSelectManyWithNavProps(true);
            CSharpFile.CloseRegion();
            CSharpFile.CloseType();
        }

        private static void GenerateWritable(string typeNamespace, bool withNavProps)
        {
            var genericEntity = Common.Names.GenericEntity;

            var summary = "This class is the base class for all Repository classes with read and write capabilities.";

            var genericParameterList = (GenericParameterList)null;
            var baseClass = (string)null;
            var typeName = (string)null;

            if(withNavProps)
            {
                summary += " Supports Navigation Properties.";
                baseClass = $"{Common.Names.ReadableNavRepositoryBase}<{genericEntity}, {Common.Names.GenericProp}>";
                typeName = Common.Names.WritableNavRepositoryBase;
                genericParameterList = new GenericParameterList { new GenericParameter { Name = genericEntity }, QueryGenerator._genericNavProp };
            }
            else
            {
                summary += " No Navigation Property support";
                baseClass = $"{Common.Names.ReadableRepositoryBase}<{genericEntity}, {Common.Names.GenericProp}>";
                typeName = Common.Names.WritableRepositoryBase;
                genericParameterList = new GenericParameterList { new GenericParameter { Name = genericEntity }, QueryGenerator._genericProp };
            }

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                BaseClass = baseClass,
                GenericParams = genericParameterList,
                IsPartial = true,
                Summary = new List<string>(1) { summary }
            };

            CSharpFile.OpenClass(type);

            InsertMethods.Generate();
            UpdateMethods.Generate();
            UpdateOrCreateMethods.Generate();
            DeleteMethods.Generate();

            GenerateWritableStoreQueries();

            CSharpFile.CloseType();
        }

        private static void GenerateWritableStoreQueries()
        {
            CSharpFile.OpenRegion("Store Queries");
            StoreQueries.GenerateDeleteAny(false);
            StoreQueries.GenerateDeleteAny(true);

            StoreQueries.GenerateUpdateEntity(false);
            StoreQueries.GenerateUpdateEntity(true);

            StoreQueries.GenerateUpdateAny(false);
            StoreQueries.GenerateUpdateAny(true);

            StoreQueries.GenerateUpdateOptimisticEntity(false);
            StoreQueries.GenerateUpdateOptimisticEntity(true);

            StoreQueries.GenerateUpdateOptimisticBatch(false);
            StoreQueries.GenerateUpdateOptimisticBatch(true);

            StoreQueries.GenerateUpdateOrCreateEntity(false);
            StoreQueries.GenerateUpdateOrCreateEntity(true);

            StoreQueries.GenerateInsert(false);
            StoreQueries.GenerateInsert(true);

            CSharpFile.CloseRegion();
        }

        private static string GenerateSelectManyCall(bool isAsync, bool supportsNavProps, bool hasNavProps, bool defaultSort, string defaultSortBy, bool supportsPagination, bool supportsFilter, bool supportsTop, bool returnsValue, int listCapacity)
        {
            var top = supportsTop ? RepoMethodSignatures.ParamNames._topCount : "0";
            var queryFilters = supportsFilter ? RepoMethodSignatures.ParamNames._queryFilters : "null";
            var sortFilters = defaultSort ? defaultSortBy : RepoMethodSignatures.ParamNames._sortFilters;
            var pagination = supportsPagination ? RepoMethodSignatures.ParamNames._paginationSettings : "null";
            var navProps = hasNavProps ? RepoMethodSignatures.ParamNames._navProps : "null";
            var returns = ComposeReturnStatementPart(isAsync, returnsValue);
            var asyncSuffix = isAsync ? "Async" : "";
            var methodName = $"{RepoMethodNames.SelectMany}{asyncSuffix}";

            if(supportsNavProps)
            {
                if(hasNavProps)
                    return $"{returns}{methodName}({queryFilters}, {sortFilters}, {navProps}, {pagination}, {top}, {listCapacity});";
                else
                    return $"{returns}{methodName}({queryFilters}, {sortFilters}, {pagination}, {top}, {listCapacity});";
            }
            else
            {
                return $"{returns}{methodName}({queryFilters}, {sortFilters}, {pagination}, {top}, {listCapacity});";
            }
        }
        private static string ComposeReturnStatementPart(bool isAsync, bool returnsValue)
        {
            if(isAsync)
                return returnsValue ? "return await " : "await ";

            return returnsValue ? "return " : "";
        }

        internal static class GetTopMethods
        {
            public static void GenerateNoNavProps()
            {
                CSharpFile.OpenRegion(RepoMethodNames.GetTop);

                //NO QUERY JUST TOP
                //Sync
                var noQueryMethodSync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetTopNoQuery(null, false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.Line(Helpers.GenerateSelectManyCallNoQuery(false, false, true, -1));
                CSharpFile.CloseBlock();

                //Async
                var noQueryMethodAsync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetTopNoQuery(null, true, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.Line(Helpers.GenerateSelectManyCallNoQuery(true, false, true, -1));
                CSharpFile.CloseBlock();

                //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)
                //Sync
                RepoMethodSignatures.RetrieveMethods.GenerateGenericGetTopBasedOnQuery(false, false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.If("query == null");
                CSharpFile.ReturnIndented(noQueryMethodSync.GenerateCall());
                CSharpFile.Line(Helpers.GenerateSelectManyCallBasedOnQuery(false, false, false, true, -1));
                CSharpFile.CloseBlock();

                //Async
                RepoMethodSignatures.RetrieveMethods.GenerateGenericGetTopBasedOnQuery(true, false, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.If("query == null");
                CSharpFile.ReturnIndented(noQueryMethodAsync.GenerateCall(true));
                CSharpFile.Line(Helpers.GenerateSelectManyCallBasedOnQuery(true, false, false, true, -1));
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
        }
        internal static class GetFirstOrDefaultMethods
        {
            public static void GenerateNoNavProps()
            {
                CSharpFile.OpenRegion(RepoMethodNames.GetFirstOrDefault);

                var methodSync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetFirstOrDefaultBasedOnQuery(false, false, false, false);
                CSharpFile.OpenBlock();
                GenerateBody(methodSync);
                CSharpFile.CloseBlock();

                var methodAsync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetFirstOrDefaultBasedOnQuery(true, false, false, false);
                CSharpFile.OpenBlock();
                GenerateBody(methodAsync);
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateBody(MethodDeclaration method)
            {
                var asyncSuffix = method.IsAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.GetTop}{asyncSuffix}";
                method.Name = methodName;
                method.Parameters.Insert(0, new MethodParameter { Name = "1" });

                CSharpFile.Line($"var result = {method.GenerateCall()}");
                CSharpFile.If("result?.Count > 0");
                CSharpFile.ReturnIndented("result[0]");
                CSharpFile.Return("default(TEntity)");
            }
        }
        internal static class GetFirstMethods
        {
            public static void GenerateNoNavProps()
            {
                CSharpFile.OpenRegion(RepoMethodNames.GetFirst);

                //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)              
                var methodSync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetFirstBasedOnQuery(false, false, false, false);
                CSharpFile.OpenBlock();
                GenerateBody(methodSync);
                CSharpFile.CloseBlock();

                var methodAsync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetFirstBasedOnQuery(true, false, false, false);
                CSharpFile.OpenBlock();
                GenerateBody(methodAsync);
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateBody(MethodDeclaration method)
            {
                var asyncSuffix = method.IsAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.GetFirstOrDefault}{asyncSuffix}";
                method.Name = methodName;

                CSharpFile.Line($"var result = {method.GenerateCall()}");
                CSharpFile.If("result == null");
                CSharpFile.LineIndented("throw new InvalidOperationException(\"No entity was found for this query.\")");
                CSharpFile.Return("result");
            }
        }
        internal static class GetSingleOrDefaultMethods
        {
            public static void GenerateNoNavProps()
            {
                CSharpFile.OpenRegion(RepoMethodNames.GetSingleOrDefault);

                var methodSync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetSingleOrDefaultBasedOnQuery(false, false, false, false);
                CSharpFile.OpenBlock();
                GenerateBody(methodSync);
                CSharpFile.CloseBlock();

                var methodAsync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetSingleOrDefaultBasedOnQuery(true, false, false, false);
                CSharpFile.OpenBlock();
                GenerateBody(methodAsync);
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateBody(MethodDeclaration method)
            {
                var asyncSuffix = method.IsAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.GetTop}{asyncSuffix}";
                method.Name = methodName;
                method.Parameters.Insert(0, new MethodParameter { Name = "2" });

                CSharpFile.Line($"var result = {method.GenerateCall()}");
                CSharpFile.If("result?.Count == 1");
                CSharpFile.ReturnIndented("result[0]");
                CSharpFile.If("result?.Count > 1");
                CSharpFile.LineIndented("throw new InvalidOperationException(\"More than one entity was found for this query.\")");
                CSharpFile.Return("default(TEntity)");
            }
        }
        internal static class GetSingleMethods
        {
            public static void GenerateNoNavProps()
            {
                CSharpFile.OpenRegion(RepoMethodNames.GetSingle);

                var methodSync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetSingleBasedOnQuery(false, false, false, false);
                CSharpFile.OpenBlock();
                GenerateBody(methodSync);
                CSharpFile.CloseBlock();

                var methodAsync = RepoMethodSignatures.RetrieveMethods.GenerateGenericGetSingleBasedOnQuery(true, false, false, false);
                CSharpFile.OpenBlock();
                GenerateBody(methodAsync);
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateBody(MethodDeclaration method)
            {
                var asyncSuffix = method.IsAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.GetSingleOrDefault}{asyncSuffix}";
                method.Name = methodName;

                CSharpFile.Line($"var result = {method.GenerateCall()}");
                CSharpFile.If("result == null");
                CSharpFile.LineIndented("throw new InvalidOperationException(\"No entity was found for this query.\")");
                CSharpFile.Return("result");
            }
        }
        internal static class GetListMethods
        {
            public static void GenerateGetListNoNavProps()
            {
                CSharpFile.OpenRegion(RepoMethodNames.GetList);
                //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)
                //Sync
                RepoMethodSignatures.RetrieveMethods.GenerateGenericGetListBasedOnQuery(false, false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.Line(Helpers.GenerateSelectManyCallBasedOnQuery(false, false, true, true, 0));
                CSharpFile.CloseBlock();

                //Async
                RepoMethodSignatures.RetrieveMethods.GenerateGenericGetListBasedOnQuery(true, false, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.Line(Helpers.GenerateSelectManyCallBasedOnQuery(true, false, true, true, 0));
                CSharpFile.CloseBlock();

                GenerateGetAll();

                CSharpFile.CloseRegion();
            }
            private static void GenerateGetAll()
            {
                RepoMethodSignatures.RetrieveMethods.GenerateGenericGetAll(false, false, false);//no pagination
                CSharpFile.OpenBlock();
                Helpers.GenerateSelectCall(false, false, false, false, true, DefaultSortField, false, false, false);
                CSharpFile.CloseBlock();

                RepoMethodSignatures.RetrieveMethods.GenerateGenericGetAll(true, false, false, skipAwait: true);//no pagination
                CSharpFile.OpenBlock();
                Helpers.GenerateSelectCall(true, false, false, false, true, DefaultSortField, false, false, false);
                CSharpFile.CloseBlock();
            }
        }

        internal static class InsertMethods
        {
            public static void Generate()
            {
                CSharpFile.OpenRegion("Create");
                RepoMethodSignatures.CreateMethods.GenerateGenericCreate(false, false, false);
                CSharpFile.OpenBlock();

                CSharpFile.Return($"{RepoMethodNames.InsertEntity}(entity)");

                CSharpFile.CloseBlock();
                RepoMethodSignatures.CreateMethods.GenerateGenericCreate(true, false, false, skipAwait: true);
                CSharpFile.OpenBlock();

                CSharpFile.Return($"{RepoMethodNames.InsertEntity}Async(entity)");

                CSharpFile.CloseBlock();
                CSharpFile.CloseRegion();

                //Create List
                GenerateCreateList();
            }

            private static void GenerateCreateList()
            {
                var entityCollection = "entities";

                CSharpFile.OpenRegion("Create List");

                RepoMethodSignatures.CreateMethods.GenerateGenericCreateList(false, false);
                CSharpFile.OpenBlock();

                CSharpFile.If($"{entityCollection} == null");
                CSharpFile.LineIndented($"throw new ArgumentNullException(\"The {entityCollection} parameter cannot be null. Failed to create list.\")");

                CSharpFile.ForEach("entity", entityCollection);
                CSharpFile.OpenBlock();
                CSharpFile.If($"!{RepoMethodNames.Create}(entity)");
                CSharpFile.LineIndented($"throw new Exception(\"Failed to create one or more {entityCollection}.\")");
                CSharpFile.CloseBlock();

                CSharpFile.CloseBlock();

                RepoMethodSignatures.CreateMethods.GenerateGenericCreateList(true, false);
                CSharpFile.OpenBlock();

                CSharpFile.If($"{entityCollection} == null");
                CSharpFile.LineIndented($"throw new ArgumentNullException(\"The {entityCollection} parameter cannot be null. Failed to create list.\")");

                CSharpFile.ForEach("entity", entityCollection);
                CSharpFile.OpenBlock();
                CSharpFile.Line($"var created = await {RepoMethodNames.Create}Async(entity)");
                CSharpFile.If("!created");
                CSharpFile.LineIndented($"throw new Exception(\"Failed to create one or more {entityCollection}.\")");
                CSharpFile.CloseBlock();
                CSharpFile.Return("true");

                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
        }
        internal static class DeleteMethods
        {
            public static void Generate()
            {
                GenerateByEntity();
                GenerateDeleteAny();
                GenerateDeleteList();
            }

            private static void GenerateByEntity()
            {
                CSharpFile.OpenRegion("DeleteEntity");

                RepoMethodSignatures.DeleteMethods.GenerateGenericDeleteByEntity(false, false, false);
                CSharpFile.OpenBlock();

                CSharpFile.Line($"var where = {RepoMethodNames.ComposeKeys}(entity)");

                CSharpFile.Return($"{RepoMethodNames.DeleteAny}(where) > 0");
                CSharpFile.CloseBlock();

                RepoMethodSignatures.DeleteMethods.GenerateGenericDeleteByEntity(true, false, false);
                CSharpFile.OpenBlock();

                CSharpFile.Line($"var where = {RepoMethodNames.ComposeKeys}(entity)");

                CSharpFile.Return($"await {RepoMethodNames.DeleteAny}Async(where) > 0");
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateDeleteList()
            {
                CSharpFile.OpenRegion("DeleteList");

                RepoMethodSignatures.DeleteMethods.GenerateGenericDeleteList(false, false);
                CSharpFile.OpenBlock();
                CSharpFile.ForEach("entity", "entities");
                CSharpFile.LineIndented($"{RepoMethodNames.Delete}(entity)");
                CSharpFile.CloseBlock();

                RepoMethodSignatures.DeleteMethods.GenerateGenericDeleteList(true, false);
                CSharpFile.OpenBlock();
                CSharpFile.ForEach("entity", "entities");
                CSharpFile.LineIndented($"await {RepoMethodNames.Delete}Async(entity)");
                CSharpFile.Return("true");
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateDeleteAny()
            {
                CSharpFile.OpenRegion("DeleteAny");

                RepoMethodSignatures.DeleteMethods.GenerateGenericDeleteAnyBasedOnQuery(false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.DeleteAny}(query.Prepare().{PredicateFiltersProp})");
                CSharpFile.CloseBlock();

                RepoMethodSignatures.DeleteMethods.GenerateGenericDeleteAnyBasedOnQuery(true, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.DeleteAny}Async(query.Prepare().{PredicateFiltersProp})");
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
        }
        internal static class UpdateMethods
        {
            public static void Generate()
            {
                GenerateUpdate();

                GenerateUpdateOptimistic();

                GenerateUpdateOptimisticBatch();

                GenerateUpdateAny();

                GenerateUpdateList();
            }
            private static void GenerateUpdate()
            {
                CSharpFile.OpenRegion(RepoMethodNames.Update);

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdate(false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateEntity}(entity)");
                CSharpFile.CloseBlock();

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdate(true, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateEntity}Async(entity)");
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateUpdateAny()
            {
                CSharpFile.OpenRegion("Update Any");

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateAnyBasedOnQuery(false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateAny}(query.Prepare().SetValues, query.Prepare().{PredicateFiltersProp})");
                CSharpFile.CloseBlock();

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateAnyBasedOnQuery(true, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateAny}Async(query.Prepare().SetValues, query.Prepare().{PredicateFiltersProp})");
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateUpdateOptimistic()
            {
                CSharpFile.OpenRegion("Update Optimistic");

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateOptimistic(false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateOptimisticEntity}(original, changed)");
                CSharpFile.CloseBlock();

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateOptimistic(true, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateOptimisticEntity}Async(original, changed)");
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateUpdateOptimisticBatch()
            {
                CSharpFile.OpenRegion("Update Optimistic Batch");

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateOptimisticBatch(false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateOptimisticBatch}(batchEntries)");
                CSharpFile.CloseBlock();

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateOptimisticBatch(true, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateOptimisticBatch}Async(batchEntries)");
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateUpdateList()
            {
                var entityCollection = "entities";

                CSharpFile.OpenRegion("Update List");

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateList(false, false);
                CSharpFile.OpenBlock();

                CSharpFile.If($"{entityCollection} == null");
                CSharpFile.LineIndented($"throw new ArgumentNullException(\"The {entityCollection} parameter cannot be null. Failed to update list.\")");
                CSharpFile.ForEach("entity", entityCollection);
                CSharpFile.LineIndented($"{RepoMethodNames.Update}(entity)");

                CSharpFile.CloseBlock();

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateList(true, false);
                CSharpFile.OpenBlock();
                CSharpFile.If($"{entityCollection} == null");
                CSharpFile.LineIndented($"throw new ArgumentNullException(\"The {entityCollection} parameter cannot be null. Failed to update list.\")");
                CSharpFile.ForEach("entity", entityCollection);
                CSharpFile.LineIndented($"await {RepoMethodNames.Update}Async(entity)");
                CSharpFile.Return("true");

                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
        }
        internal static class UpdateOrCreateMethods
        {
            public static void Generate()
            {
                CSharpFile.OpenRegion("Update Or Create");

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateOrCreate(false, false, false);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateOrCreateEntity}(entity)");
                CSharpFile.CloseBlock();

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateOrCreate(true, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.UpdateOrCreateEntity}Async(entity)");
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();

                GenerateUpdateOrCreateList();
            }
            private static void GenerateUpdateOrCreateList()
            {
                var entityCollection = "entities";

                CSharpFile.OpenRegion("Update Or Create List");

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateOrCreateList(false, false);
                CSharpFile.OpenBlock();

                CSharpFile.ForEach("entity", entityCollection);
                CSharpFile.LineIndented($"{RepoMethodNames.UpdateOrCreate}(entity)");

                CSharpFile.CloseBlock();

                RepoMethodSignatures.UpdateMethods.GenerateGenericUpdateOrCreateList(true, false);
                CSharpFile.OpenBlock();

                CSharpFile.ForEach("entity", entityCollection);
                CSharpFile.LineIndented($"await {RepoMethodNames.UpdateOrCreate}Async(entity)");
                CSharpFile.Return("true");

                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
        }

        private static class StoreQueries
        {
            public static void GenerateSelectManyNoNavProps(bool isAsync)
            {
                var genericEntity = Common.Names.GenericEntity;
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.SelectMany}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = new TypeInfo(genericEntity, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "filters", ParamType = $"List<{Common.Names.IQueryFilter}>" },
                      new MethodParameter { Name = "sortFilters", ParamType = $"List<{Common.Names.ISortFilter}>" },
                      new MethodParameter { Name = "pagination", ParamType = Common.Names.PaginationSettings },
                      new MethodParameter { Name = "topCount", ParamType = "int" },
                      new MethodParameter { Name = "listCapacity", ParamType = "int", DefaultValue = "0" },
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.If("listCapacity < 1");
                CSharpFile.LineIndented($"listCapacity = {RepoMethodNames.CalculateListCapacity}(topCount, pagination)");
                CSharpFile.Line("var resultSets = new object[1]");
                CSharpFile.Line(GenerateMaterializeInlineFunction(false, true, ", listCapacity"));

                CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
                CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
                CSharpFile.Line($"entityQuery.EntityId = {EntityIdField}");
                CSharpFile.Line("entityQuery.Top = topCount");
                CSharpFile.Line("entityQuery.Changes = null");
                CSharpFile.Line("entityQuery.Where = filters");
                CSharpFile.Line("entityQuery.Sort = sortFilters");
                CSharpFile.Line("entityQuery.Pagination = pagination");
                CSharpFile.Line($"entityQuery.NavProps = null");

                var await = isAsync ? "await " : "";

                CSharpFile.Line($"{await}{SqlEngineName}.{RepoMethodNames.DatabaseSelect}{asyncSuffix}(entityQuery, materialize)");
                CSharpFile.Line($"var result = {RepoMethodNames.BuildEntityTree}(resultSets);//build the tree AFTER the reader is done and disposed.");

                CSharpFile.If("result?.Count > 0");
                CSharpFile.ReturnIndented("result");
                CSharpFile.Return("null");
                CSharpFile.CloseBlock();
            }
            public static void GenerateSelectManyWithNavProps(bool isAsync)
            {
                var genericEntity = Common.Names.GenericEntity;
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.SelectMany}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = new TypeInfo(genericEntity, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "filters", ParamType = $"List<{Common.Names.IQueryFilter}>" },
                      new MethodParameter { Name = "sortFilters", ParamType = $"List<{Common.Names.ISortFilter}>" },
                      new MethodParameter { Name = "navprops", ParamType = Common.Names.NavPropCollection },
                      new MethodParameter { Name = "pagination", ParamType = Common.Names.PaginationSettings },
                      new MethodParameter { Name = "topCount", ParamType = "int" },
                      new MethodParameter { Name = "listCapacity", ParamType = "int", DefaultValue = "0" },
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.If("listCapacity < 1");
                CSharpFile.LineIndented($"listCapacity = {RepoMethodNames.CalculateListCapacity}(topCount, pagination)");
                CSharpFile.Line("var navPropCount = navprops != null ? navprops.TotalCount : 0");
                CSharpFile.Line("var resultCount = 1 + navPropCount");
                CSharpFile.Line("var resultSets = new object[resultCount]");
                CSharpFile.Line("var materializers = new Func<SqlDataReader, int, object>[resultCount]");
                CSharpFile.Line($"materializers[0] = {RepoMethodNames.MaterializeEntity}");
                CSharpFile.Line("var toOneParents = (int[])null");
                CSharpFile.If("navPropCount > 0");
                CSharpFile.OpenBlock();
                CSharpFile.Line("toOneParents = new int[resultCount]");
                CSharpFile.Line($"{RepoMethodNames.GetNavPropMaterializers}(navprops, materializers, toOneParents)");
                CSharpFile.CloseBlock();
                CSharpFile.Line(GenerateMaterializeInlineFunction(true, true, ", listCapacity, 0, toOneParents"));

                CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
                CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
                CSharpFile.Line($"entityQuery.EntityId = {EntityIdField}");
                CSharpFile.Line("entityQuery.Top = topCount");
                CSharpFile.Line("entityQuery.Changes = null");
                CSharpFile.Line("entityQuery.Where = filters");
                CSharpFile.Line("entityQuery.Sort = sortFilters");
                CSharpFile.Line("entityQuery.Pagination = pagination");
                CSharpFile.Line($"entityQuery.NavProps = {RepoMethodNames.BuildNavPropInfoTree}(navprops)");

                var await = isAsync ? "await " : "";

                CSharpFile.Line($"{await}{SqlEngineName}.{RepoMethodNames.DatabaseSelect}{asyncSuffix}(entityQuery, materialize)");
                CSharpFile.Line($"var result = {RepoMethodNames.BuildEntityTree}(resultSets, navprops);//build the tree AFTER the reader is done and disposed.");

                CSharpFile.If("result?.Count > 0");
                CSharpFile.ReturnIndented("result");
                CSharpFile.Return("null");

                CSharpFile.CloseBlock();
            }
            public static void GenerateSelectSingle(bool isAsync, bool supportsNavProps)
            {
                var genericEntity = Common.Names.GenericEntity;
                var filterCollection = $"List<{Common.Names.IQueryFilter}>";
                var sortCollection = $"List<{Common.Names.ISortFilter}>";
                var navPropCollection = $"{Common.Names.NavPropCollection}";
                var queryFilterParam = RepoMethodSignatures.ParamNames._queryFilters;
                var sortFilterParam = RepoMethodSignatures.ParamNames._sortFilters;
                var navPropsParam = RepoMethodSignatures.ParamNames._navProps;
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.SelectSingle}{asyncSuffix}";
                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = new TypeInfo(genericEntity),
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = queryFilterParam, ParamType = filterCollection},
                      new MethodParameter { Name = sortFilterParam, ParamType = sortCollection},
                    }
                };

                if(supportsNavProps)
                    method.Parameters.Add(new MethodParameter { Name = navPropsParam, ParamType = navPropCollection });

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.Line($"var result = {GenerateSelectManyCall(isAsync, supportsNavProps, true, false, "sortFilters", false, true, false, false, 1)}");

                CSharpFile.If("!(result?.Count > 0)");
                CSharpFile.ReturnIndented("default(TEntity)");

                CSharpFile.If("result.Count > 1");
                CSharpFile.LineIndented("throw new InvalidOperationException($\"More than one record was found at the store for entity: {typeof(TEntity).FullName}\")");

                CSharpFile.Return("result[0]");
                CSharpFile.CloseBlock();
            }

            public static void GenerateSelectSingleWithContainerNavProps(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.SelectSingle}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = new TypeInfo(Common.Names.GenericEntity),
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    SkipAwait = isAsync,//only hands the call on: returns the inner Task
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = RepoMethodSignatures.ParamNames._queryFilters, ParamType = $"List<{Common.Names.IQueryFilter}>" },
                      new MethodParameter { Name = RepoMethodSignatures.ParamNames._sortFilters, ParamType = $"List<{Common.Names.ISortFilter}>" },
                      new MethodParameter { Name = RepoMethodSignatures.ParamNames._navProps, ParamType = $"{Common.Names.NavPropCollection}<{Common.Names.GenericProp}>" },
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.Return($"{methodName}({RepoMethodSignatures.ParamNames._queryFilters}, {RepoMethodSignatures.ParamNames._sortFilters}, {RepoMethodSignatures.ParamNames._navProps}?.Items)");
                CSharpFile.CloseBlock();
            }

            public static void GenerateSelectQuery(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.SelectQuery}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = new TypeInfo(Common.Names.GenericEntity, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Virtual,
                    IsAsync = isAsync,
                    SkipAwait = isAsync,//only hands the call on: returns the inner Task
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "query", ParamType = $"{Common.Names.QueryWrapper}<{Common.Names.GenericProp}>" },
                      new MethodParameter { Name = RepoMethodSignatures.ParamNames._paginationSettings, ParamType = Common.Names.PaginationSettings },
                      new MethodParameter { Name = "topCount", ParamType = "int" },
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.Return($"{RepoMethodNames.SelectMany}{asyncSuffix}(query.PredicateFilters, query.Sorting ?? {DefaultSortField}, {RepoMethodSignatures.ParamNames._paginationSettings}, topCount)");
                CSharpFile.CloseBlock();
            }

            public static void GenerateSelectQueryOverride(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.SelectQuery}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = new TypeInfo(Common.Names.GenericEntity, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    IsAsync = isAsync,
                    SkipAwait = isAsync,//each branch only hands the call on: returns the inner Task
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "query", ParamType = $"{Common.Names.QueryWrapper}<{Common.Names.GenericProp}>" },
                      new MethodParameter { Name = RepoMethodSignatures.ParamNames._paginationSettings, ParamType = Common.Names.PaginationSettings },
                      new MethodParameter { Name = "topCount", ParamType = "int" },
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.If("query.NavigationProperties == null");
                CSharpFile.ReturnIndented($"base.{methodName}(query, {RepoMethodSignatures.ParamNames._paginationSettings}, topCount)");
                CSharpFile.Return($"{RepoMethodNames.SelectMany}{asyncSuffix}(query.PredicateFilters, query.Sorting ?? {DefaultSortField}, query.NavigationProperties, {RepoMethodSignatures.ParamNames._paginationSettings}, topCount)");
                CSharpFile.CloseBlock();
            }

            public static void GenerateInsert(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var methodName = $"{RepoMethodNames.InsertEntity}{asyncSuffix}";
                var genericEntity = Common.Names.GenericEntity;

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = TypeInfo.Bool,
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "entity", ParamType = genericEntity},
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.If($"entity == null");
                CSharpFile.LineIndented($"throw new ArgumentNullException($\"The {{typeof({genericEntity}).FullName}} parameter cannot be null. Failed to create.\")");

                CSharpFile.Line($"var insertValues = {RepoMethodNames.ComposeInsertPredicate}(entity)");
                CSharpFile.Line("var resultSets = new object[1]");

                CSharpFile.Line(GenerateMaterializeInlineFunction(false, false, ", listCapacity: 1"));
                CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
                CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
                CSharpFile.Line($"entityQuery.EntityId = {EntityIdField}");
                //CSharpFile.Line("entityQuery.Top = topCount");
                CSharpFile.Line("entityQuery.Changes = insertValues");
                //CSharpFile.Line("entityQuery.Where = where");
                //CSharpFile.Line("entityQuery.Sort = sortFilters");
                //CSharpFile.Line("entityQuery.Pagination = pagination");
                //CSharpFile.Line($"entityQuery.NavProps = {RepoMethodNames.BuildNavPropInfoTree}(navprops)");
                CSharpFile.If(HasTriggersField);
                CSharpFile.OpenBlock();
                CSharpFile.Line($"var where = {RepoMethodNames.ComposeKeys}(entity)");
                CSharpFile.Line("entityQuery.Where = where");
                CSharpFile.CloseBlock();

                var await = isAsync ? "await " : "";

                CSharpFile.Line($"{await}{SqlEngineName}.{RepoMethodNames.DatabaseInsert}{asyncSuffix}(entityQuery, materialize)");

                //BuildEntityTree takes an optional parameter so it is no longer necessary to pass null on the second parameter.
                CSharpFile.Line($"var result = {RepoMethodNames.BuildEntityTree}(resultSets);//Build the tree AFTER the reader is done and disposed.");

                CSharpFile.If("!(result?.Count > 0) || result[0] == null");
                CSharpFile.ReturnIndented("false");
                CSharpFile.Line($"{RepoMethodNames.Merge}(result[0], entity)");
                CSharpFile.Return("true");
                CSharpFile.CloseBlock();
            }

            public static void GenerateUpdateEntity(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var genericEntity = Common.Names.GenericEntity;
                var methodName = $"{RepoMethodNames.UpdateEntity}{asyncSuffix}";
                //var queryFilterParam = RepoMethodSignatures.ParamNames._queryFilters;

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = TypeInfo.Bool,
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "entity", ParamType = genericEntity},
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.If($"entity == null");
                CSharpFile.LineIndented($"throw new ArgumentNullException($\"The {{typeof({genericEntity}).FullName}} parameter cannot be null. Failed to update.\")");

                CSharpFile.Line($"var where = {RepoMethodNames.ComposeKeys}(entity)");

                var await = isAsync ? "await " : "";
                //SelectSingle takes an optional parameter so it is no longer necessary to pass null on the second parameter.
                CSharpFile.Line($"var dbEntity = {await}{RepoMethodNames.SelectSingle}{asyncSuffix}(where, {DefaultSortField})");

                CSharpFile.If("dbEntity == null");
                CSharpFile.LineIndented($"throw new ArgumentException($\"The {{typeof({genericEntity}).FullName}} was not found in the data store. Failed to update.\")");

                CSharpFile.Return($"{await}{RepoMethodNames.UpdateOptimisticBatch}{asyncSuffix}(new List<{Common.Names.BatchEntry}<{genericEntity}>>{{ new {Common.Names.BatchEntry}<{genericEntity}> {{ Original = dbEntity, Changed = entity }}}})");

                CSharpFile.CloseBlock();
            }
            public static void GenerateUpdateOptimisticEntity(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var genericEntity = Common.Names.GenericEntity;
                var methodName = $"{RepoMethodNames.UpdateOptimisticEntity}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = TypeInfo.Bool,
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    SkipAwait = isAsync,//only hands the call on: returns the inner Task
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "original", ParamType = genericEntity},
                      new MethodParameter { Name = "changed", ParamType = genericEntity},
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.Return($"{RepoMethodNames.UpdateOptimisticBatch}{asyncSuffix}(new List<{Common.Names.BatchEntry}<{genericEntity}>>{{ new {Common.Names.BatchEntry}<{genericEntity}> {{ Original = original, Changed = changed }}}})");

                CSharpFile.CloseBlock();
            }
            public static void GenerateUpdateOptimisticBatch(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var genericEntity = Common.Names.GenericEntity;
                var batchEntry = Common.Names.BatchEntry;
                var methodName = $"{RepoMethodNames.UpdateOptimisticBatch}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = TypeInfo.Bool,
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "batchEntries", ParamType = $"List<{batchEntry}<{genericEntity}>>"},
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.If("batchEntries == null");
                CSharpFile.LineIndented("throw new ArgumentNullException(\"The batchEntries parameter cannot be null. Failed to update.\")");

                CSharpFile.If("batchEntries.Count < 1");
                CSharpFile.LineIndented("throw new ArgumentException(\"The batchEntries parameter cannot be empty. Failed to update.\")");

                CSharpFile.Line($"var changesBatch = new List<{Common.Names.RepositoryEntityQuery}>(batchEntries.Count)");
                CSharpFile.Line($"var withChanges = new List<{Common.Names.BatchEntry}<{genericEntity}>>(batchEntries.Count)");

                CSharpFile.ForEach("entry", "batchEntries");
                CSharpFile.OpenBlock();
                CSharpFile.Line("var original = entry.Original");
                CSharpFile.Line("var changed = entry.Changed");
                CSharpFile.If("original == null");
                CSharpFile.LineIndented("throw new NullReferenceException(\"The Original property cannot be null. Failed to update.\")");
                CSharpFile.If("changed == null");
                CSharpFile.LineIndented("throw new NullReferenceException(\"The Changed property cannot be null. Failed to update.\")");
                CSharpFile.Line($"var changes = {RepoMethodNames.GetChanges}(original, changed)");
                CSharpFile.If("changes == null");
                CSharpFile.ContinueIndented();
                CSharpFile.Line($"var where = {RepoMethodNames.ComposeKeys}(original)");
                CSharpFile.Line($"var changeEntry = new {Common.Names.RepositoryEntityQuery}()");
                CSharpFile.Line($"changeEntry.ModelId = {ModelIdField}");
                CSharpFile.Line($"changeEntry.EntityId = {EntityIdField}");
                CSharpFile.Line("changeEntry.Changes = changes");
                CSharpFile.Line("changeEntry.Where = where");
                CSharpFile.Line("changesBatch.Add(changeEntry)");
                CSharpFile.Line("withChanges.Add(entry)");

                CSharpFile.CloseBlock();

                CSharpFile.If("changesBatch.Count < 1");
                CSharpFile.ReturnIndented("false");

                CSharpFile.Line("var resultSets = new object[changesBatch.Count]");

                CSharpFile.Line("var materializers = new Func<SqlDataReader, int, object>[changesBatch.Count]");

                CSharpFile.LineNoEnding("for(var i = 0; i < resultSets.Length; i++)");
                CSharpFile.LineIndented($"materializers[i] = {RepoMethodNames.MaterializeEntity}");

                //here we say true because it will take an array of materializers
                CSharpFile.Line(GenerateMaterializeInlineFunction(true, false, ", listCapacity: 1, otherListCapacity: 1"));

                var await = isAsync ? "await " : "";

                CSharpFile.Line($"{await}{SqlEngineName}.{RepoMethodNames.DatabaseUpdate}{asyncSuffix}(changesBatch, materialize)");

                CSharpFile.Line($"var results = new List<{genericEntity}>(resultSets.Length)");

                CSharpFile.LineNoEnding("for(var i = 0; i < resultSets.Length; i++)");
                CSharpFile.OpenBlock();
                CSharpFile.Line($"var result = resultSets[i] as List<{genericEntity}>");
                CSharpFile.If("result == null || result.Count < 1");
                CSharpFile.LineIndented("results.Add(default)");
                CSharpFile.Else();
                CSharpFile.LineIndented("results.Add(result[0])");
                CSharpFile.CloseBlock();

                CSharpFile.If("results.Count < 1");
                CSharpFile.ReturnIndented("false");

                CSharpFile.Line("var resultCount = results.Count");
                CSharpFile.LineNoEnding("for(var i = 0; i < resultCount; i++)");
                CSharpFile.OpenBlock();
                CSharpFile.Line($"var changed = results[i]");
                CSharpFile.If("changed == null");
                CSharpFile.LineIndented("continue");
                CSharpFile.Line($"{RepoMethodNames.Merge}(changed, withChanges[i].Original)");
                CSharpFile.Line($"{RepoMethodNames.Merge}(changed, withChanges[i].Changed)");
                CSharpFile.CloseBlock();

                CSharpFile.Return("true");
                CSharpFile.CloseBlock();
            }

            public static void GenerateUpdateAny(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var genericEntity = Common.Names.GenericEntity;
                var filterCollection = $"List<{Common.Names.IQueryFilter}>";
                var valuesToSetParam = RepoMethodSignatures.ParamNames._valuesToSet;
                var queryFilterParam = RepoMethodSignatures.ParamNames._queryFilters;
                var methodName = $"{RepoMethodNames.UpdateAny}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    AccessModifier = AccessModifiers.Protected,
                    ReturnType = new TypeInfo(genericEntity, isList: true),
                    IsAsync = isAsync,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = valuesToSetParam, ParamType = filterCollection },
                      new MethodParameter { Name = queryFilterParam, ParamType = filterCollection }
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.Line("var resultSets = new object[1]");

                CSharpFile.Line(GenerateMaterializeInlineFunction(false, false, ""));
                CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
                CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
                CSharpFile.Line($"entityQuery.EntityId = {EntityIdField}");
                //CSharpFile.Line("entityQuery.Top = topCount");
                CSharpFile.Line($"entityQuery.Changes = {valuesToSetParam}");
                CSharpFile.Line($"entityQuery.Where = {queryFilterParam}");
                //CSharpFile.Line("entityQuery.Sort = sortFilters");
                //CSharpFile.Line("entityQuery.Pagination = pagination");
                //CSharpFile.Line($"entityQuery.NavProps = {RepoMethodNames.BuildNavPropInfoTree}(navprops)");

                var await = isAsync ? "await " : "";

                CSharpFile.Line($"{await}{SqlEngineName}.{RepoMethodNames.DatabaseUpdate}{asyncSuffix}(new List<{Common.Names.RepositoryEntityQuery}> {{ entityQuery }}, materialize)");

                //BuildEntityTree takes an optional parameter so it is no longer necessary to pass null on the second parameter.

                CSharpFile.Line($"var result = {RepoMethodNames.BuildEntityTree}(resultSets);", "Build the tree AFTER the reader is done and disposed.");

                CSharpFile.If("result?.Count > 0");
                CSharpFile.ReturnIndented("result");
                CSharpFile.Return("null");
                CSharpFile.CloseBlock();
            }
            public static void GenerateUpdateOrCreateEntity(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var genericEntity = Common.Names.GenericEntity;
                var methodName = $"{RepoMethodNames.UpdateOrCreateEntity}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = TypeInfo.Bool,
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "entity", ParamType = genericEntity}
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.If($"entity == null");
                CSharpFile.LineIndented($"throw new ArgumentNullException($\"The {{typeof({genericEntity}).FullName}} parameter cannot be null. Failed to update or create.\")");

                CSharpFile.Line($"var where = {RepoMethodNames.ComposeKeys}(entity)");

                var await = isAsync ? "await " : "";

                CSharpFile.Line($"var dbEntity = {await}{RepoMethodNames.SelectSingle}{asyncSuffix}(where, {DefaultSortField})");

                CSharpFile.If("dbEntity == null");
                CSharpFile.ReturnIndented($"{await}{RepoMethodNames.InsertEntity}{asyncSuffix}(entity)");
                CSharpFile.Return($"{await}{RepoMethodNames.UpdateOptimisticBatch}{asyncSuffix}(new List<{Common.Names.BatchEntry}<{genericEntity}>>{{ new {Common.Names.BatchEntry}<{genericEntity}> {{ Original = dbEntity, Changed = entity }}}})");
                CSharpFile.CloseBlock();
            }

            public static void GenerateDeleteAny(bool isAsync)
            {
                var asyncSuffix = isAsync ? "Async" : "";
                var queryFilterParam = RepoMethodSignatures.ParamNames._queryFilters;
                var methodName = $"{RepoMethodNames.DeleteAny}{asyncSuffix}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = TypeInfo.Int,
                    AccessModifier = AccessModifiers.Protected,
                    IsAsync = isAsync,
                    SkipAwait = isAsync,//builds the query, then only hands the call on: returns the inner Task
                    Parameters = new List<MethodParameter> { new MethodParameter { Name = queryFilterParam, ParamType = $"List<{Common.Names.IQueryFilter}>" } }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
                CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
                CSharpFile.Line($"entityQuery.EntityId = {EntityIdField}");
                //CSharpFile.Line("entityQuery.Top = topCount");
                //CSharpFile.Line($"entityQuery.Changes = {valuesToSetParam}");
                CSharpFile.Line($"entityQuery.Where = {queryFilterParam}");
                //CSharpFile.Line("entityQuery.Sort = sortFilters");
                //CSharpFile.Line("entityQuery.Pagination = pagination");
                //CSharpFile.Line($"entityQuery.NavProps = {RepoMethodNames.BuildNavPropInfoTree}(navprops)");

                CSharpFile.Return($"{SqlEngineName}.{RepoMethodNames.DatabaseDelete}{asyncSuffix}(entityQuery)");
                CSharpFile.CloseBlock();
            }
        }
        private static class Materializers
        {
            public static void GenerateBuildEntityTreeNoNavProps()
            {
                var genericEntity = Common.Names.GenericEntity;

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.BuildEntityTree,
                    ReturnType = new TypeInfo(genericEntity, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    IsStatic = true,
                    Parameters = new List<MethodParameter> { new MethodParameter { Name = "resultSets", ParamType = "object[]" } }
                };

                CSharpFile.DeclareMethod(method);

                CSharpFile.OpenBlock();
                CSharpFile.If("!(resultSets?.Length > 0) || resultSets[0] == null");
                CSharpFile.ReturnIndented("null");
                CSharpFile.Return($"resultSets[0] as List<{genericEntity}>");
                CSharpFile.CloseBlock();

            }
            public static void GenerateBuildEntityTreeWithNavProps()
            {
                var genericEntity = Common.Names.GenericEntity;

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.BuildEntityTree,
                    ReturnType = new TypeInfo(genericEntity, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "resultSets", ParamType = "object[]" },
                      new MethodParameter { Name = "navprops", ParamType = Common.Names.NavPropCollection }
                    }
                };

                CSharpFile.DeclareMethod(method);

                CSharpFile.OpenBlock();
                CSharpFile.If("!(resultSets?.Length > 0) || resultSets[0] == null");
                CSharpFile.ReturnIndented("null");
                CSharpFile.Line($"var entityList = resultSets[0] as List<{genericEntity}>");
                CSharpFile.If("resultSets.Length == 1");
                CSharpFile.ReturnIndented("entityList");
                CSharpFile.If("navprops.Count > 0");
                CSharpFile.LineIndented($"{RepoMethodNames.BuildObjectGraph}(entityList, resultSets, navprops)");
                CSharpFile.Return("entityList");
                CSharpFile.CloseBlock();
            }
            public static void GenerateMaterializeResult()
            {
                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.MaterializeResult,
                    ReturnType = TypeInfo.Void,
                    AccessModifier = AccessModifiers.Protected,
                    IsStatic = true,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "r", ParamType = "SqlDataReader" },
                      new MethodParameter { Name = "materializers", ParamType = "Func<SqlDataReader, int, object>[]" },
                      new MethodParameter { Name = "resultSets", ParamType = "object[]" },
                      new MethodParameter { Name = "pagination", ParamType = Common.Names.PaginationSettings, DefaultValue = "null" },
                      new MethodParameter { Name = "listCapacity", ParamType = "int", DefaultValue = "0" },
                      new MethodParameter { Name = "otherListCapacity", ParamType = "int", DefaultValue = "0" },
                      new MethodParameter { Name = "toOneParents", ParamType = "int[]", DefaultValue = "null" }
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Line("var resultIndex = 0");
                CSharpFile.Line("var keepReading = true");
                CSharpFile.While("keepReading");
                CSharpFile.OpenBlock();
                CSharpFile.Line("var capacity = resultIndex == 0 ? listCapacity : otherListCapacity");
                CSharpFile.Comment("a to-one navigation property returns at most one row per row of its parent result set, which was read before it");
                CSharpFile.If("resultIndex > 0 && toOneParents != null && toOneParents[resultIndex] >= 0");
                CSharpFile.LineIndented($"capacity = {RepoMethodNames.ToOneCapacity}(resultSets[toOneParents[resultIndex]])");
                CSharpFile.Line($"{RepoMethodNames.MaterializeResult}(r, materializers[resultIndex], resultSets, resultIndex, pagination, capacity)");
                CSharpFile.Line("keepReading = r.NextResult()");
                CSharpFile.Line("resultIndex++");
                CSharpFile.CloseBlock();
                CSharpFile.CloseBlock();

                var toOneCapacity = new MethodDeclaration
                {
                    Name = RepoMethodNames.ToOneCapacity,
                    ReturnType = TypeInfo.Int,
                    AccessModifier = AccessModifiers.Private,
                    IsStatic = true,
                    Parameters = new List<MethodParameter> { new MethodParameter { Name = "parentResult", ParamType = "object" } }
                };

                CSharpFile.DeclareMethod(toOneCapacity);
                CSharpFile.OpenBlock();
                CSharpFile.If("!(parentResult is System.Collections.ICollection parentRows) || parentRows.Count < 1");
                CSharpFile.Indent();
                CSharpFile.Line("return 1", "no parent rows: no rows here either, and the list is only created for a first row");
                CSharpFile.Outdent();
                CSharpFile.Return("parentRows.Count > _maxListCapacity ? _maxListCapacity : parentRows.Count");
                CSharpFile.CloseBlock();

                method.Parameters[1] = new MethodParameter { Name = "materializer", ParamType = "Func<SqlDataReader, int, object>" };
                method.Parameters.RemoveAt(6);
                method.Parameters.RemoveAt(5);

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Line($"{RepoMethodNames.MaterializeResult}(r, materializer, resultSets, 0, pagination, listCapacity)");
                CSharpFile.CloseBlock();

                method.AccessModifier = AccessModifiers.Private;
                method.Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "r", ParamType = "SqlDataReader" },
                  new MethodParameter { Name = "materializer", ParamType = "Func<SqlDataReader, int, object>" },
                  new MethodParameter { Name = "resultSets", ParamType = "object[]" },
                  new MethodParameter { Name = "resultIndex", ParamType = "int" },
                  new MethodParameter { Name = "pagination", ParamType = Common.Names.PaginationSettings, DefaultValue = null },
                  new MethodParameter { Name = "listCapacity", ParamType = "int", DefaultValue = null }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Comment("a paged query always reads the total row count with the rows: the rows come back with it, whether or not the total was asked for");
                CSharpFile.Line($"var materialized = materializer(r, listCapacity)");
                CSharpFile.If("materialized is object[] array");
                CSharpFile.OpenBlock();
                CSharpFile.If("pagination != null && pagination.GetTotalCount");
                CSharpFile.LineIndented("pagination.TotalCount = (int)array[0]");
                CSharpFile.Line("resultSets[resultIndex] = array[1]");
                CSharpFile.CloseBlock();
                CSharpFile.Else();
                CSharpFile.OpenBlock();
                CSharpFile.Line("resultSets[resultIndex] = materialized");
                CSharpFile.CloseBlock();
                CSharpFile.CloseBlock();
            }

            public static void GenerateListCapacity()
            {
                CSharpFile.Line("private static readonly int _unboundedListCapacity = 500", "a query with no TOP and no page can return a few rows or thousands");
                CSharpFile.Line("private static readonly int _maxListCapacity = 10000", "a larger TOP or page size starts the list here, and it grows from there");

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.CalculateListCapacity,
                    ReturnType = TypeInfo.Int,
                    AccessModifier = AccessModifiers.Protected,
                    IsStatic = true,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "topCount", ParamType = "int" },
                      new MethodParameter { Name = "pagination", ParamType = Common.Names.PaginationSettings }
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Comment("the most rows the query can return: its TOP or its page size, whichever is smaller; 0 when it has neither");
                CSharpFile.Line("var bound = topCount > 0 ? topCount : 0");
                CSharpFile.If("pagination != null && pagination.PageSize > 0 && (bound == 0 || pagination.PageSize < bound)");
                CSharpFile.LineIndented("bound = pagination.PageSize");
                CSharpFile.If("bound > _maxListCapacity");
                CSharpFile.ReturnIndented("_maxListCapacity");
                CSharpFile.Return("bound");
                CSharpFile.CloseBlock();
            }

            public static void GenerateMaterializeEntityResult()
            {
                var genericEntity = Common.Names.GenericEntity;
                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.MaterializeEntityResult,
                    ReturnType = TypeInfo.Object,
                    AccessModifier = AccessModifiers.Protected,
                    IsStatic = true,
                    GenericParams = new GenericParameterList { new GenericParameter { Name = genericEntity } },
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "r", ParamType = "SqlDataReader" },
                      new MethodParameter { Name = "materializer", ParamType = $"Func<SqlDataReader, {genericEntity}>" },
                      new MethodParameter { Name = "fieldCount", ParamType = "int" },
                      new MethodParameter { Name = "listCapacity", ParamType = "int" }
                    }
                };
                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.If("!r.Read()");
                CSharpFile.Indent();
                CSharpFile.Line("return null", "no rows: no list");
                CSharpFile.Outdent();
                CSharpFile.Line($"var result = new List<{genericEntity}>(listCapacity > 0 ? listCapacity : _unboundedListCapacity)", "a key lookup, a TOP, a page size or a to-one parent's row count sets the size; with none the list starts at 500 rows and grows from there");
                CSharpFile.Line($"var totalRowCount = 0;//this is used for pagination represents the total number of rows returned if no pagination was being used");
                CSharpFile.Comment("if the field count is greater than the column count get the total number of rows from the last column");
                CSharpFile.If($"r.FieldCount > fieldCount");
                CSharpFile.LineIndented("totalRowCount = r.GetInt32(fieldCount)");
                CSharpFile.Do();
                CSharpFile.OpenBlock();
                CSharpFile.Line($"result.Add(materializer(r))");
                CSharpFile.CloseBlock();
                CSharpFile.DoWhile("r.Read()");
                CSharpFile.If("totalRowCount > 0");
                CSharpFile.ReturnIndented("new object[] { totalRowCount, result };//return both the row count for pagination and the results");
                CSharpFile.Return("result");
                CSharpFile.CloseBlock();
            }

            public static void GenerateUnboxEntityList()
            {
                var genericEntity = Common.Names.GenericEntity;

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.UnboxEntityList,
                    ReturnType = new TypeInfo(genericEntity, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    IsStatic = true,
                    GenericParams = new GenericParameterList { new GenericParameter { Name = genericEntity } },
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "result", ParamType = "object" }
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.If($"result == null");
                CSharpFile.ReturnIndented("null");
                CSharpFile.If($"!(result is List<{genericEntity}> list)");
                CSharpFile.Indent();
                CSharpFile.ThrowArgumentException("$\"Unable to convert type {result.GetType().Name} to List<{typeof(" + genericEntity + ").FullName}>.\"");
                CSharpFile.Outdent();
                CSharpFile.Return("list");
                CSharpFile.CloseBlock();
            }
            public static void GenerateAddEntityToList()
            {
                var genericEntity = Common.Names.GenericEntity;

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.AddEntityToList,
                    ReturnType = new TypeInfo(genericEntity, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    IsStatic = true,
                    GenericParams = new GenericParameterList { new GenericParameter { Name = genericEntity } },
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "list", ParamType = $"List<{genericEntity}>" },
                      new MethodParameter { Name = "entity", ParamType = genericEntity }
                    }
                };
                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.If($"list == null");
                CSharpFile.LineIndented($"list = new List<{genericEntity}>()");
                CSharpFile.Line($"list.Add(entity)");
                CSharpFile.Return("list");
                CSharpFile.CloseBlock();
            }
            public static void GenerateMaterializeXmlProperty()
            {
                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.MaterializeXmlProperty,
                    ReturnType = new TypeInfo("XDocument"),
                    AccessModifier = AccessModifiers.Protected,
                    IsStatic = true,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "r", ParamType = "SqlDataReader" },
                      new MethodParameter { Name = "index", ParamType = "int" }
                    }
                };
                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Line("var sqlXml = r.GetSqlXml(index)");
                CSharpFile.If("sqlXml.IsNull");
                CSharpFile.ReturnIndented("null");
                CSharpFile.Line("var result = (XDocument)null");
                CSharpFile.LineNoEnding("using(var xReader = sqlXml.CreateReader())");
                CSharpFile.OpenBlock();
                CSharpFile.Line("result = XDocument.Load(xReader)");
                CSharpFile.Line("xReader.Close()");
                CSharpFile.CloseBlock();
                CSharpFile.Return("result");
                CSharpFile.CloseBlock();
            }
        }
    }

    private static class StoredProcBaseClass
    {
        public static void Generate(string typeNamespace)
        {
            var typeName = Common.Names.StoredProcRepositoryBase;

            var summary = "This class is the base class for all Stored Procedure Repository classes.";

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                IsPartial = true,
                Summary = new List<string> { summary }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.OpenRegion("Protected Properties");
            PropertyExtensions.Auto(TypeInfo.Int, ModelIdField, AccessModifiers.Protected);
            PropertyExtensions.FullNoSetter(TypeInfo.Int, "ReturnParamId", AccessModifiers.Protected, "Database.ReturnParamId", true);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Protected Methods");
            var method = new MethodDeclaration
            {
                Name = RepoMethodNames.StoredProcedure,
                ReturnType = new TypeInfo("Dictionary<int, object>"),
                AccessModifier = AccessModifiers.Protected,
                IsAsync = false,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "procId", ParamType = "int" },
                  new MethodParameter { Name = "filters", ParamType = "List<IQueryFilter>" },
                  new MethodParameter { Name = "materialize", ParamType = "Action<SqlDataReader>" },
                }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
            CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
            CSharpFile.Line($"entityQuery.EntityId = procId");
            CSharpFile.Line($"entityQuery.Where = filters");
            CSharpFile.Return($"{SqlEngineName}.{RepoMethodNames.DatabaseStoredProc}(entityQuery, materialize)");
            CSharpFile.CloseBlock();

            method.Name = $"{RepoMethodNames.StoredProcedure}Async";
            method.IsAsync = true;
            method.SkipAwait = true;//builds the query, then only hands the call on: returns the inner Task

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
            CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
            CSharpFile.Line($"entityQuery.EntityId = procId");
            CSharpFile.Line($"entityQuery.Where = filters");
            CSharpFile.Return($"{SqlEngineName}.{RepoMethodNames.DatabaseStoredProc}Async(entityQuery, materialize)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseRegion();
            CSharpFile.CloseType();
        }
    }
    private static class FuncBaseClass
    {
        public static void Generate(string typeNamespace)
        {
            var typeName = Common.Names.FuncRepositoryBase;

            var summary = "This class is the base class for all Function Repository classes.";

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                IsPartial = true,
                Summary = new List<string> { summary }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.OpenRegion("Protected Properties");
            PropertyExtensions.Auto(TypeInfo.Int, ModelIdField, AccessModifiers.Protected);
            CSharpFile.CloseRegion();
            //Function(int modelId, int entityId, bool isScalar, SqlParameter[] param, Action<SqlDataReader> materializer)
            CSharpFile.OpenRegion("Protected Methods");
            var method = new MethodDeclaration
            {
                Name = RepoMethodNames.Function,
                ReturnType = TypeInfo.Void,
                AccessModifier = AccessModifiers.Protected,
                IsAsync = false,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "funcId", ParamType = "int" },
                  new MethodParameter { Name = "isScalar", ParamType = "bool" },
                  new MethodParameter { Name = "param", ParamType = "SqlParameter[]" },
                  new MethodParameter { Name = "materialize", ParamType = "Action<SqlDataReader>" },
                }
            };
            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"{SqlEngineName}.{RepoMethodNames.DatabaseFunction}({ModelIdField}, funcId, isScalar, param, materialize)");
            CSharpFile.CloseBlock();

            method.Name = $"{RepoMethodNames.Function}Async";
            method.IsAsync = true;
            method.SkipAwait = true;//only hands the call on: returns the inner Task
            method.ReturnType = new TypeInfo("bool");

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"return {SqlEngineName}.{RepoMethodNames.DatabaseFunction}Async({ModelIdField}, funcId, isScalar, param, materialize)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseRegion();
            CSharpFile.CloseType();
        }
    }
    private static class SeqBaseClass
    {
        public static void Generate(string typeNamespace)
        {
            var typeName = Common.Names.SeqRepositoryBase;

            var summary = "This class is the base class for all Sequence Repository classes.";

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                InheritanceModifier = InheritanceModifiers.Abstract,
                IsPartial = true,
                Summary = new List<string> { summary }
            };

            CSharpFile.OpenClass(type);

            CSharpFile.OpenRegion("Protected Properties");
            PropertyExtensions.Auto(TypeInfo.Int, ModelIdField, AccessModifiers.Protected);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Protected Methods");
            var method = new MethodDeclaration
            {
                Name = RepoMethodNames.Sequence,
                ReturnType = new TypeInfo("TResult"),
                AccessModifier = AccessModifiers.Protected,
                GenericParams = new GenericParameterList { new GenericParameter { Name = "TResult" } },
                IsAsync = false,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "seqId", ParamType = "int" },
                }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
            CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
            CSharpFile.Line($"entityQuery.EntityId = seqId");
            CSharpFile.Return($"{SqlEngineName}.{RepoMethodNames.DatabaseSequence}<TResult>(entityQuery)");
            CSharpFile.CloseBlock();

            method.Name = $"{RepoMethodNames.Sequence}Async";
            method.IsAsync = true;
            method.SkipAwait = true;//builds the query, then only hands the call on: returns the inner Task

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.Line($"var entityQuery = new {Common.Names.RepositoryEntityQuery}()");
            CSharpFile.Line($"entityQuery.ModelId = {ModelIdField}");
            CSharpFile.Line($"entityQuery.EntityId = seqId");
            CSharpFile.Return($"{SqlEngineName}.{RepoMethodNames.DatabaseSequence}Async<TResult>(entityQuery)");
            CSharpFile.CloseBlock();

            CSharpFile.CloseRegion();
            CSharpFile.CloseType();
        }
    }
}

#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    public static string Generate(ModelContainer modelContainer, Model model, Entity entity, Schema schema, bool isReadOnly)
    {
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

        var hasNavProps = entity.NPs?.Count > 0;
        var typeName = Common.GenerateRepositoryName(entity);
        var typeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Repository, model, schema);
        //var typeDeclaration = $"public partial class {typeName}";
        var repoInterfaceNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schema);
        var entityNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Entity, model, schema);
        var propEnumNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.PropEnum, model, schema);
        var navPropEnumNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.NavPropEnum, model, schema);
        var repoCommonNamespace = modelContainer.CommonLayers.Repository.FullNamespace(modelContainer.CommonSettings);
        var commonNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
        var utilsNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.EntityUtil, model, schema);
        var entityName = Common.GenerateTypeName(entity);

        var entityFullName = Common.GenerateTypeFullName(modelContainer.CommonSettings, model.Layers.Entity, model, schema, Common.GenerateTypeName(entity));

        var sqlClientNamespace = Common.CalculateSqlClientNamespace(modelContainer);

        var usingNamespaces = new List<UsingNamespace>(12)
        {
          new UsingNamespace { Name = "System" },
          new UsingNamespace { Name = "System.Collections.Generic" },
          new UsingNamespace { Name = sqlClientNamespace },
          new UsingNamespace { Name = "System.Threading.Tasks" },
          new UsingNamespace { Name = repoCommonNamespace },
          new UsingNamespace { Name = repoInterfaceNamespace },
          new UsingNamespace { Name = commonNamespace },
          new UsingNamespace { Name = propEnumNamespace },
          new UsingNamespace { Name = utilsNamespace }
        };

        var entityType = entity.N;
        var entNamespace = new UsingNamespace { Name = entityNamespace };

        if(string.CompareOrdinal(entityNamespace, typeNamespace) != 0)//if the entity is in a different namespace then add it
        {
            if(entity.FullyQualify)//if the schema name is the same as the entity we need to hack the namespace to avoid a weird visual studio issue
            {
                var alias = entity.N + "Namespace";
                entityType = $"{alias}.{entityName}";
                entNamespace.AliasRequired = true;
                entNamespace.Alias = alias;
                entNamespace.AliasComment = "//this is required because the schema's name is the same as the entity's name.";
            }

            usingNamespaces.Add(entNamespace);
        }

        var baseClass = isReadOnly ? Common.Names.ReadableRepositoryBase : Common.Names.WritableRepositoryBase;

        if(hasNavProps)
        {
            if(string.CompareOrdinal(propEnumNamespace, navPropEnumNamespace) != 0)
                usingNamespaces.Add(new UsingNamespace { Name = navPropEnumNamespace });

            foreach(var m in modelContainer.Models)
            {
                var modelNamespace = new UsingNamespace
                {
                    Alias = m.Name + "E",
                    AliasRequired = true,
                    Name = m.Layers.PropEnum.FullNamespace(modelContainer.CommonSettings) + "." + m.Name
                };
                usingNamespaces.Add(modelNamespace);
            }

            //WritableNavRepositoryBase<Order, OrderProperty>
            baseClass = isReadOnly ? Common.Names.ReadableNavRepositoryBase : Common.Names.WritableNavRepositoryBase;
            baseClass = $"{baseClass}<{entityType}, {Common.GeneratePropClassName(entity)}>";
        }
        else
        {
            //WritableRepositoryBase<Order, OrderProperty>
            baseClass = $"{baseClass}<{entityType}, {Common.GeneratePropClassName(entity)}>";
        }

        var summary = $"This class represents the Repository for: {entity.N}.";

        var type = new ClassDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            BaseClass = baseClass,
            Interfaces = new List<string> { Common.GenerateIRepositoryName(entity) },
            Summary = new List<string> { summary }
        };

        //Generate file header with information about the interface.
        CSharpFile.Header(type, Common.SharpFactoryFileMarker);

        CSharpFile.UsingStatements(usingNamespaces);

        CSharpFile.OpenClassWithNamespace(type);

        RepoBody.Generate(modelContainer.CommonSettings, model, entity, schema, entNamespace, entityFullName, hasNavProps, isReadOnly);

        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }
    private static class RepoBody
    {
        public static void Generate(CommonSettings commonSettings, Model model, Entity entity, Schema schema, UsingNamespace entityNamespace, string entityFullName, bool supportsNavProps, bool isReadOnly)
        {
            GenerateNavPropInfoDictionary(model, entity);

            GenerateDefaultConstructor(model, entity, schema);

            EntityInfoClass.GenerateAdditionalSortingMembersForRepositoryClass(entity);

            //Retrieve
            GetMethods.GenerateById(entity, entityNamespace, entityFullName, supportsNavProps);

            GetMethods.GenerateByPK(entity, entityNamespace, entityFullName, supportsNavProps);

            GetMethods.GenerateByUK(entity, entityNamespace, entityFullName, supportsNavProps);

            if(!isReadOnly)
            {
                //Delete
                DeleteMethods.Generate(entity, entityFullName);
            }

            //Materialization
            Materialization.Generate(commonSettings, model, entity, entityNamespace, supportsNavProps);
        }
        private static void GenerateDefaultConstructor(Model model, Entity entity, Schema schema)
        {
            var typeName = Common.GenerateRepositoryName(entity);
            var repoLookupEntityId = $"{Common.Names.RepositoryLookup}.{Common.Names.RepositoryLookupEntityId}";
            var repoLookupModelId = $"{Common.Names.RepositoryLookup}.{Common.Names.RepositoryLookupModelId}";

            CSharpFile.OpenRegion("Constructors");

            var constructor = new ConstructorDeclaration
            {
                Name = typeName
            };
            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.Comment("this is the unique id for the model to which this repository belongs to. used to find the correct connection string");
            CSharpFile.Line($"{ModelIdField} = {repoLookupModelId}.{model.Name}");
            CSharpFile.Comment($"this is the unique id for the entity. used to retrieve EntityInfo object from the {repoLookupEntityId}");
            CSharpFile.Line($"{EntityIdField} = {repoLookupEntityId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, schema.Name, entity.N)}");
            CSharpFile.Comment("tables with triggers have limitations that need to be accounted for");
            CSharpFile.Line($"{HasTriggersField} = {(entity.HTr == YesOrNo.Yes ? "true" : "false")}");

            EntityInfoClass.GenerateDefaultSortingMembersForRepositoryClass(entity);
            CSharpFile.CloseBlock();

            CSharpFile.CloseRegion();
        }
        private static void GenerateNavPropInfoDictionary(Model model, Entity entity)
        {
            if(!(entity.NPs?.Count > 0))
                return;

            var repoLookupEntityId = $"{Common.Names.RepositoryLookup}.{Common.Names.RepositoryLookupEntityId}";
            var navPropEnum = Common.GenerateNavPropEnumName(entity);
            var navPropInfo = Common.Names.RepositoryNavPropertyInfo;
            var navPropPair = Common.Names.RepositoryNavPropertyPair;
            //var navPropEqualityComparer = Common.GenerateEqualityComparer(navPropEnum);
            var entities = model.Categories.GetByName(CategoryNames.Entities);
            var dicType = $"Dictionary<{navPropEnum}, {navPropInfo}>";

            CSharpFile.OpenRegion("Navigation Property Info");
            var rootEnumNamespace = model.Name + "E.";//this uses an alias to reduce the size
            CSharpFile.Comment($"Contains information about the Navigation Properties for this entity.");
            CSharpFile.AppendIndented($"private static readonly {dicType} {NavPropInfosField} = new {dicType}({entity.NPs.Count}){{ ");
            foreach(var nav in entity.NPs)
            {
                var fk = model.FKeys.GetById(nav.FKId);
                if(!(fk.FKParts?.Count > 0))
                    continue;

                var isParent = nav.End == NavPropertyEnd.Parent;//if this navproperty is the parent. meaning the current entity is the child

                //assume is parent
                var child = entity;
                var parent = entities.Schemas.GetByName(fk.PSc).Entities.GetByName(fk.PEn);
                var otherSchema = fk.PSc;
                var otherEntity = fk.PEn;

                //if not parent then flip it around
                if(!isParent)
                {
                    parent = entity;
                    child = entities.Schemas.GetByName(fk.CSc).Entities.GetByName(fk.CEn);
                    otherSchema = fk.CSc;
                    otherEntity = fk.CEn;
                }

                var entityIdField = $"{repoLookupEntityId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, otherSchema, otherEntity)}";
                var childEnumType = rootEnumNamespace + fk.CSc + "." + Common.GeneratePropEnumName(fk.CEn) + ".";
                var parentEnumType = rootEnumNamespace + fk.PSc + "." + Common.GeneratePropEnumName(fk.PEn) + ".";

                CSharpFile.Append($" {{ {navPropEnum}.{nav.N}, new {navPropInfo}{{ {EntityInfoClass.NavPropertyInfoProperties.EntityId} = {entityIdField}, {EntityInfoClass.NavPropertyInfoProperties.IsParent} = {(isParent ? "true" : "false")}, {EntityInfoClass.NavPropertyInfoProperties.Predicate} = new List<{navPropPair}>({fk.FKParts.Count}){{");
                foreach(var k in fk.FKParts)
                {
                    if(isParent)//if the other end is parent put the child first. it means the current entity is the child.
                    {
                        CSharpFile.Append($" new {navPropPair}{{ {EntityInfoClass.NavPropertyPairProperties.PropId} = (int){childEnumType}{child.Ps.GetByStoreName(k.CCol).N}, {EntityInfoClass.NavPropertyPairProperties.OtherPropId} = (int){parentEnumType}{parent.Ps.GetByStoreName(k.PCol).N}, }},");
                    }
                    else//if the other end is child put the parent first. it means the current entity is the parent.
                    {
                        CSharpFile.Append($" new {navPropPair}{{ {EntityInfoClass.NavPropertyPairProperties.PropId} = (int){parentEnumType}{parent.Ps.GetByStoreName(k.PCol).N}, {EntityInfoClass.NavPropertyPairProperties.OtherPropId} = (int){childEnumType}{child.Ps.GetByStoreName(k.CCol).N}, }},");
                    }
                }

                CSharpFile.Append("} } },");
            }

            CSharpFile.Append(" };");
            CSharpFile.EmptyLine();
            CSharpFile.CloseRegion();
        }
        internal static class GetMethods
        {
            public static void GenerateById(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool supportsNavProps)
            {
                if(entity.HId == YesOrNo.No)
                    return;//if there is no identity then there is nothing to generate

                if(entity.PK?.Ps?.Count == 1)
                {
                    for(var i = 0; i < entity.Ps.Count; i++)
                    {
                        if(entity.Ps[i].Ide == YesOrNo.Yes)
                        {
                            if(string.CompareOrdinal(entity.Ps[i].N, entity.PK?.Ps[0].N) == 0)
                                return;//if the PK and the Identity are the same there is no need to have two methods to do the same thing
                        }
                    }
                }

                CSharpFile.OpenRegion(RepoMethodNames.GetById);
                RepoMethodSignatures.RetrieveMethods.GenerateGetById(entity, entityNamespace, false, entityFullName, false, false);
                CSharpFile.OpenBlock();
                GenerateGetByIdBody(entity, false, supportsNavProps, false);//no nav props
                CSharpFile.CloseBlock();

                RepoMethodSignatures.RetrieveMethods.GenerateGetById(entity, entityNamespace, true, entityFullName, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                GenerateGetByIdBody(entity, true, supportsNavProps, false);//no nav props
                CSharpFile.CloseBlock();

                if(supportsNavProps)//if it has navprops then create 2 overloads. one with nav props and one without.
                {
                    RepoMethodSignatures.RetrieveMethods.GenerateGetById(entity, entityNamespace, false, entityFullName, false, true);
                    CSharpFile.OpenBlock();
                    GenerateGetByIdBody(entity, false, supportsNavProps, true);//with nav props
                    CSharpFile.CloseBlock();

                    RepoMethodSignatures.RetrieveMethods.GenerateGetById(entity, entityNamespace, true, entityFullName, false, true, skipAwait: true);
                    CSharpFile.OpenBlock();
                    GenerateGetByIdBody(entity, true, supportsNavProps, true);//with nav props
                    CSharpFile.CloseBlock();
                }

                CSharpFile.CloseRegion();
            }
            public static void GenerateByPK(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool supportsNavProps)
            {
                if(entity.PK == null || entity.PK.Ps.Count < 1)
                    return;//if there is no PrimaryKey then there is nothing to generate

                CSharpFile.OpenRegion(RepoMethodNames.GetByPK);
                RepoMethodSignatures.RetrieveMethods.GenerateGetByPK(entity, entityNamespace, false, entityFullName, false, false);
                CSharpFile.OpenBlock();
                GenerateGetByPKBody(entity, false, supportsNavProps, false);//no nav props
                CSharpFile.CloseBlock();

                RepoMethodSignatures.RetrieveMethods.GenerateGetByPK(entity, entityNamespace, true, entityFullName, false, false, skipAwait: true);
                CSharpFile.OpenBlock();
                GenerateGetByPKBody(entity, true, supportsNavProps, false);//no nav props
                CSharpFile.CloseBlock();

                if(supportsNavProps)//if it has navprops then create 2 overloads. one with nav props and one without.
                {
                    RepoMethodSignatures.RetrieveMethods.GenerateGetByPK(entity, entityNamespace, false, entityFullName, false, true);
                    CSharpFile.OpenBlock();
                    GenerateGetByPKBody(entity, false, supportsNavProps, true);//with nav props
                    CSharpFile.CloseBlock();

                    RepoMethodSignatures.RetrieveMethods.GenerateGetByPK(entity, entityNamespace, true, entityFullName, false, true, skipAwait: true);
                    CSharpFile.OpenBlock();
                    GenerateGetByPKBody(entity, true, supportsNavProps, true);//with nav props
                    CSharpFile.CloseBlock();
                }

                CSharpFile.CloseRegion();
            }
            public static void GenerateByUK(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool supportsNavProps)
            {
                if(entity.UKs == null || entity.UKs.Count < 1)
                    return;//if there is no Unique Keys then there is nothing to generate
                CSharpFile.OpenRegion(RepoMethodNames.GetByUK);

                var keyCount = 0;
                var hasSingleKey = entity.UKs.Count == 1;
                foreach(var uk in entity.UKs)
                {
                    if(uk.Ps == null || uk.Ps.Count < 1)
                        continue;

                    keyCount++;//this is the suffix to differentiate the method names.

                    var keySuffix = hasSingleKey ? "" : keyCount.ToString();
                    RepoMethodSignatures.RetrieveMethods.GenerateGetByUK(entity, entityNamespace, false, uk, entityFullName, keySuffix, false, false);
                    CSharpFile.OpenBlock();
                    GenerateGetByUKBody(entity, uk, false, supportsNavProps, false);//no nav props
                    CSharpFile.CloseBlock();

                    RepoMethodSignatures.RetrieveMethods.GenerateGetByUK(entity, entityNamespace, true, uk, entityFullName, keySuffix, false, false, skipAwait: true);
                    CSharpFile.OpenBlock();
                    GenerateGetByUKBody(entity, uk, true, supportsNavProps, false);//no nav props
                    CSharpFile.CloseBlock();

                    if(supportsNavProps)//if it has navprops then create 2 overloads. one with nav props and one without.
                    {
                        RepoMethodSignatures.RetrieveMethods.GenerateGetByUK(entity, entityNamespace, false, uk, entityFullName, keySuffix, false, true);
                        CSharpFile.OpenBlock();
                        GenerateGetByUKBody(entity, uk, false, supportsNavProps, true);//with nav props
                        CSharpFile.CloseBlock();

                        RepoMethodSignatures.RetrieveMethods.GenerateGetByUK(entity, entityNamespace, true, uk, entityFullName, keySuffix, false, true, skipAwait: true);
                        CSharpFile.OpenBlock();
                        GenerateGetByUKBody(entity, uk, true, supportsNavProps, true);//with nav props
                        CSharpFile.CloseBlock();
                    }
                }

                CSharpFile.CloseRegion();
            }

            private static void GenerateGetByIdBody(Entity entity, bool isAsync, bool supportsNavProps, bool hasNavProps)
            {//this method is only called if there is a Id present          
                var queryFilterCollection = $"List<{Common.Names.IQueryFilter}>";
                var queryFilter = Common.Names.QueryFilter;
                var containerName = Common.GeneratePropClassName(entity);
                var conditionEnum = Common.Names.FilterConditionsEnum;

                CSharpFile.AppendIndented($"var {RepoMethodSignatures.ParamNames._queryFilters} = new {queryFilterCollection}(1){{ ");
                foreach(var p in entity.Ps)
                {
                    if(p.Ide == YesOrNo.Yes)
                    {
                        CSharpFile.Append($"{queryFilter}.New({containerName}.{p.N}, {conditionEnum}.Equals, {p.N.ToLower()} ), }}");
                        break;
                    }
                }

                CSharpFile.CloseLine();
                Helpers.GenerateSelectCall(isAsync, true, supportsNavProps, hasNavProps, true, DefaultSortField, false, true, false);
            }
            private static void GenerateGetByPKBody(Entity entity, bool isAsync, bool supportsNavProps, bool hasNavProps)
            {//this method is only called if there is a PK present
                var queryFilterCollection = $"List<{Common.Names.IQueryFilter}>";
                var queryFilter = Common.Names.QueryFilter;
                var containerName = Common.GeneratePropClassName(entity);
                var conditionEnum = Common.Names.FilterConditionsEnum;

                CSharpFile.AppendIndented($"var {RepoMethodSignatures.ParamNames._queryFilters} = new {queryFilterCollection}({entity.PK.Ps.Count}){{ ");
                foreach(var p in entity.PK.Ps)
                    CSharpFile.Append($"{queryFilter}.New({containerName}.{p.N}, {conditionEnum}.Equals, {p.N.ToLower()} ), ");

                CSharpFile.CloseLine("}");
                Helpers.GenerateSelectCall(isAsync, true, supportsNavProps, hasNavProps, true, DefaultSortField, false, true, false);
            }
            private static void GenerateGetByUKBody(Entity entity, Key uk, bool isAsync, bool supportsNavProps, bool hasNavProps)
            {//this method is only called if there is a UK present        
                var queryFilterCollection = $"List<{Common.Names.IQueryFilter}>";
                var queryFilter = Common.Names.QueryFilter;
                var containerName = Common.GeneratePropClassName(entity);
                var conditionEnum = Common.Names.FilterConditionsEnum;

                CSharpFile.Comment($"this method uses the UniqueKey {uk.N}");
                CSharpFile.AppendIndented($"var {RepoMethodSignatures.ParamNames._queryFilters} = new {queryFilterCollection}({uk.Ps.Count}){{ ");
                foreach(var p in uk.Ps)
                    CSharpFile.Append($"{queryFilter}.New({containerName}.{p.N}, {conditionEnum}.Equals, {p.N.ToLower()} ), ");

                CSharpFile.CloseLine("}");

                var sortFilters = GenerateSortByFieldName(uk);
                Helpers.GenerateSelectCall(isAsync, true, supportsNavProps, hasNavProps, true, sortFilters, false, true, false);
            }
        }

        internal static class DeleteMethods
        {
            public static void Generate(Entity entity, string entityFullName)
            {
                GenerateById(entity, entityFullName);
                GenerateByPK(entity, entityFullName);
                GenerateByUK(entity, entityFullName);
            }
            private static void GenerateById(Entity entity, string entityFullName)
            {
                if(entity.HId == YesOrNo.No)
                    return;//if there is no identity then there is nothing to generate
                if(entity.PK?.Ps?.Count == 1)
                {
                    for(var i = 0; i < entity.Ps.Count; i++)
                    {
                        if(entity.Ps[i].Ide == YesOrNo.Yes)
                        {
                            if(string.CompareOrdinal(entity.Ps[i].N, entity.PK?.Ps[0].N) == 0)
                                return;//if the PK and the Identity are the same there is no need to have two methods to do the same thing
                        }
                    }
                }

                CSharpFile.OpenRegion("DeleteById");

                RepoMethodSignatures.DeleteMethods.GenerateDeleteById(entity, false, entityFullName, false);
                CSharpFile.OpenBlock();
                GenerateDeleteByIdBody(entity, false);
                CSharpFile.CloseBlock();

                RepoMethodSignatures.DeleteMethods.GenerateDeleteById(entity, true, entityFullName, false);
                CSharpFile.OpenBlock();
                GenerateDeleteByIdBody(entity, true);
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateByPK(Entity entity, string entityFullName)
            {
                if(entity.PK == null || entity.PK.Ps.Count < 1)
                    return;//if there is no PrimaryKey then there is nothing to generate

                CSharpFile.OpenRegion("DeleteByPK");

                RepoMethodSignatures.DeleteMethods.GenerateDeleteByPK(entity, false, entityFullName, false);
                CSharpFile.OpenBlock();
                GenerateDeleteByPKBody(entity, false);
                CSharpFile.CloseBlock();

                RepoMethodSignatures.DeleteMethods.GenerateDeleteByPK(entity, true, entityFullName, false);
                CSharpFile.OpenBlock();
                GenerateDeleteByPKBody(entity, true);
                CSharpFile.CloseBlock();

                CSharpFile.CloseRegion();
            }
            private static void GenerateByUK(Entity entity, string entityFullName)
            {
                if(entity.UKs == null || entity.UKs.Count < 1)
                    return;//if there is no Unique Keys then there is nothing to generate
                CSharpFile.OpenRegion("DeleteByUK");
                var keyCount = 0;
                var hasSingleKey = entity.UKs.Count == 1;

                foreach(var uk in entity.UKs)
                {
                    if(uk.Ps == null || uk.Ps.Count < 1)
                        continue;

                    keyCount++;//this is the suffix to differentiate the method names.

                    var keySuffix = hasSingleKey ? "" : keyCount.ToString();
                    RepoMethodSignatures.DeleteMethods.GenerateDeleteByUK(entity, uk, false, entityFullName, keySuffix, false);
                    CSharpFile.OpenBlock();
                    GenerateDeleteByUKBody(entity, uk, false);
                    CSharpFile.CloseBlock();

                    RepoMethodSignatures.DeleteMethods.GenerateDeleteByUK(entity, uk, true, entityFullName, keySuffix, false);
                    CSharpFile.OpenBlock();
                    GenerateDeleteByUKBody(entity, uk, true);
                    CSharpFile.CloseBlock();
                }

                CSharpFile.CloseRegion();
            }
            private static void GenerateDeleteByIdBody(Entity entity, bool isAsync)
            {//this method is only called if there is a Id present
                var queryFilterCollection = $"List<{Common.Names.IQueryFilter}>";
                var queryFilter = Common.Names.QueryFilter;
                var containerName = Common.GeneratePropClassName(entity);
                var conditionEnum = Common.Names.FilterConditionsEnum;

                CSharpFile.AppendIndented($"var {RepoMethodSignatures.ParamNames._queryFilters} = new {queryFilterCollection}(1){{ ");
                foreach(var p in entity.Ps)
                {
                    if(p.Ide == YesOrNo.Yes)
                    {
                        CSharpFile.Append($"{queryFilter}.New({containerName}.{p.N}, {conditionEnum}.Equals, {p.N.ToLower()}), }}");
                        break;
                    }
                }

                var await = isAsync ? "await " : "";
                var asyncSuffix = isAsync ? "Async" : "";

                CSharpFile.CloseLine();
                CSharpFile.Return($"{await}{RepoMethodNames.DeleteAny}{asyncSuffix}({RepoMethodSignatures.ParamNames._queryFilters}) > 0");
            }
            private static void GenerateDeleteByPKBody(Entity entity, bool isAsync)
            {//this method is only called if there is a PK present
                var queryFilterCollection = $"List<{Common.Names.IQueryFilter}>";
                var queryFilter = Common.Names.QueryFilter;
                var containerName = Common.GeneratePropClassName(entity);
                var conditionEnum = Common.Names.FilterConditionsEnum;

                CSharpFile.AppendIndented($"var {RepoMethodSignatures.ParamNames._queryFilters} = new {queryFilterCollection}({entity.PK.Ps.Count}){{ ");

                foreach(var p in entity.PK.Ps)
                    CSharpFile.Append($"{queryFilter}.New({containerName}.{p.N}, {conditionEnum}.Equals, {p.N.ToLower()}), ");
                CSharpFile.CloseLine("}");

                var await = isAsync ? "await " : "";
                var asyncSuffix = isAsync ? "Async" : "";
                CSharpFile.Return($"{await}{RepoMethodNames.DeleteAny}{asyncSuffix}({RepoMethodSignatures.ParamNames._queryFilters}) > 0");
            }
            private static void GenerateDeleteByUKBody(Entity entity, Key uk, bool isAsync)
            {//this method is only called if there is a UK present
                var queryFilterCollection = $"List<{Common.Names.IQueryFilter}>";
                var queryFilter = Common.Names.QueryFilter;
                var containerName = Common.GeneratePropClassName(entity);
                var conditionEnum = Common.Names.FilterConditionsEnum;

                CSharpFile.Comment($"this method uses the UniqueKey {uk.N}");
                CSharpFile.AppendIndented($"var {RepoMethodSignatures.ParamNames._queryFilters} = new {queryFilterCollection}({uk.Ps.Count}){{ ");

                foreach(var p in uk.Ps)
                    CSharpFile.Append($"{queryFilter}.New({containerName}.{p.N}, {conditionEnum}.Equals, {p.N.ToLower()}), ");
                CSharpFile.CloseLine("}");

                var await = isAsync ? "await " : "";
                var asyncSuffix = isAsync ? "Async" : "";
                CSharpFile.Return($"{await}{RepoMethodNames.DeleteAny}{asyncSuffix}({RepoMethodSignatures.ParamNames._queryFilters}) > 0");
            }
        }
        internal static class Materialization
        {
            private static readonly string _invalidNavPropExceptionMsg = "$\"NavigationProperty {np} is not valid.\"";
            public static void Generate(CommonSettings commonSettings, Model model, Entity entity, UsingNamespace entityNamespace, bool supportsNavProps)
            {//type comparisson speeds: http://higherlogics.blogspot.ca/2013/09/clr-cost-of-dynamic-type-tests.html              
                var props = entity.Ps;
                if(props == null || props.Count < 1)
                    return;

                CSharpFile.OpenRegion("Materialization");

                GenerateComposeKeys(entity, entityNamespace);

                GenerateGetChanges(entity, entityNamespace);

                GenerateMerge(entity, entityNamespace);

                GenerateInsertPredicate(entity, entityNamespace);

                GenerateMaterialize(props, entity, entityNamespace);

                if(supportsNavProps)//these methods only exist if there is navproperties
                {
                    #region GenerateBuildNavPropInfos
                    GenerateBuildNavPropInfoTree();
                    GenerateBuildNavPropInfos(commonSettings, model, entity);
                    #endregion

                    #region GetNavPropMaterializer           
                    GenerateGetNavPropMaterializers();
                    GenerateComposeNavPropMaterializers(commonSettings, model, entity);
                    #endregion

                    #region BuildObjectGraph
                    GenerateBuildObjectGraph(entity, entityNamespace);
                    GeneratePopulateNavProperties(commonSettings, model, entity, entityNamespace);
                    #endregion
                }

                CSharpFile.CloseRegion();
            }

            private static void GenerateMaterialize(PropertyCollection props, Entity entity, UsingNamespace entityNamespace)
            {
                var entityName = entity.N;
                var entityType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

                var propCount = props.Count;

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.MaterializeEntity,
                    ReturnType = TypeInfo.Object,
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "r", ParamType = "SqlDataReader" },
                      new MethodParameter { Name = "listCapacity", ParamType = "int" }
                    }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.Materialize}(r, listCapacity)");
                CSharpFile.CloseBlock();

                method.Name = RepoMethodNames.Materialize;
                method.AccessModifier = AccessModifiers.Internal;
                method.InheritanceModifier = InheritanceModifiers.None;
                method.IsStatic = true;

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.MaterializeEntityResult}(r, {RepoMethodNames.MaterializeSingleEntity}, {props.Count}, listCapacity)");
                CSharpFile.CloseBlock();

                method.Name = RepoMethodNames.MaterializeSingleEntity;
                method.ReturnType = new TypeInfo(entityType);
                method.AccessModifier = AccessModifiers.Private;
                method.Parameters = new List<MethodParameter> { new MethodParameter { Name = "r", ParamType = "SqlDataReader" } };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.ReturnNoEnding($"new {entityType}");
                CSharpFile.OpenBlock();
                for(var i = 0; i < propCount; i++)
                {
                    MaterializeProperty(props[i], i);
                }

                CSharpFile.CloseBlockWithSemicolon();

                CSharpFile.CloseBlock();
            }

            private static void GenerateBuildNavPropInfoTree()
            {
                var navPropInfo = Common.Names.RepositoryNavPropertyInfo;

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.BuildNavPropInfoTree,
                    ReturnType = new TypeInfo(navPropInfo, isList: true),
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "navprops", ParamType = Common.Names.NavPropCollection }
                    },
                    Summary = new List<string> { "Composes the Navigation Property tree sent to the Database class." }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Return($"{RepoMethodNames.BuildNavPropInfos}(navprops)");
                CSharpFile.CloseBlock();
            }
            private static void GenerateBuildNavPropInfos(CommonSettings commonSettings, Model model, Entity entity)
            {
                var navPropInfo = Common.Names.RepositoryNavPropertyInfo;
                var navPropEnum = Common.GenerateNavPropEnumName(entity);

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.BuildNavPropInfos,
                    ReturnType = new TypeInfo(navPropInfo, isList: true),
                    AccessModifier = AccessModifiers.Internal,
                    IsStatic = true,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "navprops", ParamType = Common.Names.NavPropCollection }
                    },
                    Summary = new List<string> { "Composes the Navigation Property tree sent to the Database class." }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.If("!(navprops?.Count > 0)");
                CSharpFile.ReturnIndented("null");

                CSharpFile.Line($"var result = new List<{navPropInfo}>(navprops.Count)");
                CSharpFile.ForEach("p", "navprops");
                CSharpFile.OpenBlock();
                CSharpFile.Line($"var np = ({navPropEnum})p.Value");
                CSharpFile.Line("var hasChildNavProps = p.NavProps?.Count > 0");
                CSharpFile.Line($"var npInfo = hasChildNavProps ? {NavPropInfosField}[np].Clone() : {NavPropInfosField}[np]");
                CSharpFile.Line($"result.Add(npInfo)");
                CSharpFile.If($"!hasChildNavProps");
                CSharpFile.LineIndented("continue");
                CSharpFile.Switch("np");
                CSharpFile.OpenBlock();
                foreach(var p in entity.NPs)
                {
                    var fk = model.FKeys.GetById(p.FKId);
                    if(!(fk.FKParts?.Count > 0))
                        continue;

                    var otherSchemaName = fk.CSc;
                    var otherEntityName = fk.CEn;

                    if(p.End == NavPropertyEnd.Parent)//if parent flip it around
                    {
                        otherSchemaName = fk.PSc;
                        otherEntityName = fk.PEn;
                    }

                    var otherSchema = model.Categories.GetByName(CategoryNames.Entities).Schemas.GetByName(otherSchemaName);

                    var navPropNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.Repository, model, otherSchema);
                    var repoName = Common.GenerateRepositoryName(otherSchema.Entities.GetByName(otherEntityName));

                    CSharpFile.Case($"{navPropEnum}.{p.N}");
                    CSharpFile.LineIndented($"npInfo.NavProps = {navPropNamespace}.{repoName}.{RepoMethodNames.BuildNavPropInfos}(p.NavProps)");
                    CSharpFile.Break();
                }

                CSharpFile.CaseDefault();
                CSharpFile.ThrowArgumentException(_invalidNavPropExceptionMsg);
                CSharpFile.CloseBlock();
                CSharpFile.CloseBlock();
                CSharpFile.Return("result");
                CSharpFile.CloseBlock();
            }

            private static void GenerateGetNavPropMaterializers()
            {
                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.GetNavPropMaterializers,
                    ReturnType = TypeInfo.Void,
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "navprops", ParamType = Common.Names.NavPropCollection, Description = "The navigation properties of the query." },
                      new MethodParameter { Name = "materializers", ParamType = "Func<SqlDataReader, int, object>[]", Description = "One materializer per result set; result set 0 is the entity itself." },
                      new MethodParameter { Name = "toOneParents", ParamType = "int[]", Description = "Per result set: the parent result set of a to-one navigation property, -1 otherwise." }
                    },
                    Summary = new List<string> { "Composes the Navigation Property tree materializers." }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Line($"{RepoMethodNames.ComposeNavPropMaterializers}(navprops, materializers, toOneParents, 0, 1)", "result set 0 is the entity itself");
                CSharpFile.CloseBlock();
            }
            private static void GenerateComposeNavPropMaterializers(CommonSettings commonSettings, Model model, Entity entity)
            {
                var navPropEnum = Common.GenerateNavPropEnumName(entity);

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.ComposeNavPropMaterializers,
                    ReturnType = TypeInfo.Int,
                    ReturnTypeForComments = "The index of the next result set.",
                    AccessModifier = AccessModifiers.Internal,
                    IsStatic = true,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "navprops", ParamType = Common.Names.NavPropCollection, Description = "The navigation properties of this entity in the query." },
                      new MethodParameter { Name = "materializers", ParamType = "Func<SqlDataReader, int, object>[]", Description = "One materializer per result set." },
                      new MethodParameter { Name = "toOneParents", ParamType = "int[]", Description = "Per result set: the parent result set of a to-one navigation property, -1 otherwise." },
                      new MethodParameter { Name = "parentIndex", ParamType = "int", Description = "The result set of this entity." },
                      new MethodParameter { Name = "index", ParamType = "int", Description = "The result set of the first navigation property." }
                    },
                    Summary = new List<string> { "Composes the Navigation Property tree materializers, in the order of the result sets (each parent before its children)." }
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.If("!(navprops?.Count > 0)");
                CSharpFile.ReturnIndented("index");

                CSharpFile.ForEach("p", "navprops");
                CSharpFile.OpenBlock();
                CSharpFile.Line($"var np = ({navPropEnum})p.Value");

                CSharpFile.Switch("np");
                CSharpFile.OpenBlock();
                foreach(var p in entity.NPs)
                {
                    var fk = model.FKeys.GetById(p.FKId);
                    if(!(fk.FKParts?.Count > 0))
                        continue;

                    var otherSchemaName = fk.CSc;
                    var otherEntityName = fk.CEn;

                    if(p.End == NavPropertyEnd.Parent)//if parent then flip it around
                    {
                        otherSchemaName = fk.PSc;
                        otherEntityName = fk.PEn;
                    }

                    var otherSchema = model.Categories.GetByName(CategoryNames.Entities).Schemas.GetByName(otherSchemaName);

                    var navPropNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.Repository, model, otherSchema);
                    var repoName = Common.GenerateRepositoryName(otherSchema.Entities.GetByName(otherEntityName));

                    //the same multiplicity PopulateNavProperties uses: a to-one navigation property is assigned, not added to a list
                    var isToOne = p.End == NavPropertyEnd.Parent ? fk.PMu[0] == '1' : fk.CMu[0] != '*';

                    CSharpFile.Case($"{navPropEnum}.{p.N}");
                    CSharpFile.Line($"materializers[index] = {navPropNamespace}.{repoName}.{RepoMethodNames.Materialize}");
                    if(isToOne)
                        CSharpFile.Line("toOneParents[index] = parentIndex", "to-one: at most one row per parent row");
                    else
                        CSharpFile.Line("toOneParents[index] = -1");
                    CSharpFile.Line("index++");
                    CSharpFile.If("p.NavProps?.Count > 0");
                    CSharpFile.LineIndented($"index = {navPropNamespace}.{repoName}.{RepoMethodNames.ComposeNavPropMaterializers}(p.NavProps, materializers, toOneParents, index - 1, index)");
                    CSharpFile.Break();
                }

                CSharpFile.CaseDefault();
                CSharpFile.ThrowArgumentException(_invalidNavPropExceptionMsg);
                CSharpFile.CloseBlock();

                CSharpFile.CloseBlock();
                CSharpFile.Return("index");
                CSharpFile.CloseBlock();
            }

            private static void GenerateBuildObjectGraph(Entity entity, UsingNamespace entityNamespace)
            {
                var entityName = entity.N;
                var entityType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.BuildObjectGraph,
                    ReturnType = TypeInfo.Void,
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "entities", ParamType = $"List<{entityType}>" },
                      new MethodParameter { Name = "results", ParamType = "object[]" },
                      new MethodParameter { Name = "navprops", ParamType = Common.Names.NavPropCollection }
                    },
                    Summary = new List<string> { "Populates the object graph with the materialized results." }
                };
                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Line($"{RepoMethodNames.PopulateNavProperties}(entities, results, navprops)");
                CSharpFile.CloseBlock();
            }
            private static void GeneratePopulateNavProperties(CommonSettings commonSettings, Model model, Entity entity, UsingNamespace entityNamespace)
            {
                var entityName = entity.N;
                var entityType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
                //var entityFullName = Common.GenerateTypeFullName(commonSettings, model.Layers.Entity, model, schema, Common.GenerateTypeName(entity));
                var navPropEnum = Common.GenerateNavPropEnumName(entity);
                //var navPropCollection = Common.Names.NavPropCollection();
                var returnType = "int";

                var keyInfos = ComposeKeyInfo(commonSettings, model, entity, entityNamespace);

                var ukSuffixByName = new Dictionary<string, string>(keyInfos.Count);

                var addedMethods = new HashSet<string>(ukSuffixByName.Count);
                var hasSingleKey = entity.UKs?.Count == 0;
                var keyCount = 0;

                if(keyInfos.Count > 0)
                {
                    foreach(var ki in keyInfos)
                    {
                        if(ki.IsNavPropTheParent)
                            continue;

                        var methodName = string.Empty;
                        var comment = string.Empty;

                        if(ki.IsPK)
                        {
                            methodName = RepoMethodNames.ComposeDictionaryByPK;
                            comment = "creates a dictionary by Primary Key";
                        }
                        else
                        {
                            keyCount++;

                            var keySuffix = hasSingleKey ? "" : keyCount.ToString();

                            if(!ukSuffixByName.ContainsKey(ki.Key.N))
                                ukSuffixByName.Add(ki.Key.N, keySuffix);

                            var key = ki.Key;

                            methodName = $"{RepoMethodNames.ComposeDictionaryByUK}{keySuffix}";
                            comment = $"creates a dictionary by Unique Key: {key.N}";
                        }

                        if(addedMethods.Contains(methodName))
                            continue;

                        addedMethods.Add(methodName);

                        GenerateComposeDictionaryByKeyMethod(ki, entityNamespace, methodName, comment);
                    }
                }

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.PopulateNavProperties,
                    ReturnType = new TypeInfo(returnType),
                    AccessModifier = AccessModifiers.Internal,
                    IsStatic = true,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "entities", ParamType = $"List<{entityType}>" },
                      new MethodParameter { Name = "results", ParamType = "object[]" },
                      new MethodParameter { Name = "navprops", ParamType = Common.Names.NavPropCollection },
                      new MethodParameter { Name = "resultsetindex", ParamType = "int", DefaultValue = "0" }
                    },
                    Summary = new List<string> { "Populates the object graph with the materialized results." }
                };
                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();

                CSharpFile.If("results?.Length < resultsetindex + 1");
                CSharpFile.ReturnIndented("resultsetindex");

                var addedDictionaries = new HashSet<string>(ukSuffixByName.Count);
                if(keyInfos.Count > 0)
                {
                    foreach(var ki in keyInfos)
                    {
                        if(ki.IsNavPropTheParent)
                            continue;

                        var dictName = ki.IsPK ? "entByPK" : $"entByUK{ukSuffixByName[ki.Key.N]}";
                        if(addedDictionaries.Contains(dictName))
                            continue;

                        addedDictionaries.Add(dictName);

                        CSharpFile.Line($"var {dictName} = ({Common.Names.MultiKeyDictionary}<{ki.KeyTypes}>)null");
                    }
                }

                CSharpFile.ForEach("navprop", "navprops");
                CSharpFile.OpenBlock();

                CSharpFile.Line($"resultsetindex++");
                CSharpFile.Line($"var np = ({navPropEnum})navprop.Value");
                CSharpFile.Line($"var result = results[resultsetindex]");

                CSharpFile.Switch("np");
                CSharpFile.OpenBlock();
                Helpers.GenerateSwitchForAssociateNavPropResult(commonSettings, model, keyInfos, ukSuffixByName, navPropEnum);
                CSharpFile.CloseBlock();

                CSharpFile.CloseBlock();

                CSharpFile.Return("resultsetindex");

                CSharpFile.CloseBlock();
            }
            private static List<KeyInfo> ComposeKeyInfo(CommonSettings commonSettings, Model model, Entity entity, UsingNamespace entityNamespace)
            {
                if(!(entity.NPs?.Count > 0))
                    return new List<KeyInfo>(0);
                var result = new List<KeyInfo>(entity.NPs.Count);

                foreach(var np in entity.NPs)
                {
                    var fk = model.FKeys.GetById(np.FKId);
                    if(!(fk.FKParts?.Count > 0))
                        continue;

                    var keyInfo = new KeyInfo
                    {
                        FK = fk,
                        NavProp = np,
                        IsNavPropTheParent = np.End == NavPropertyEnd.Parent
                    };

                    var otherSchemaName = fk.CSc;
                    var otherEntityName = fk.CEn;

                    if(keyInfo.IsNavPropTheParent)
                    {
                        otherSchemaName = fk.PSc;
                        otherEntityName = fk.PEn;
                    }

                    var otherSchema = model.Categories.GetByName(CategoryNames.Entities).Schemas.GetByName(otherSchemaName);
                    var otherEntity = otherSchema.Entities.GetByName(otherEntityName);
                    var otherEntityFullName = Common.GenerateTypeFullName(commonSettings, model.Layers.Entity, model, otherSchema, otherEntity.N);

                    keyInfo.Entity = entity;

                    var entityName = entity.N;
                    var entityType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

                    keyInfo.OtherEntity = otherEntity;

                    //var otherAlias = entityName + "Namespace";
                    //var otherEntityType = !otherEntity.FullyQualify ? otherEntityName : $"{otherAlias}.{otherEntityName}";

                    keyInfo.OtherSchema = otherSchema;
                    keyInfo.OtherEntityFullName = otherEntityFullName;

                    if(keyInfo.IsNavPropTheParent)
                    {
                        keyInfo.KeyEntity = otherEntity;
                        keyInfo.KeyEntityFullName = otherEntityFullName;
                    }
                    else
                    {
                        keyInfo.KeyEntity = entity;
                        keyInfo.KeyEntityFullName = entityType;
                    }

                    keyInfo.IsPK = keyInfo.KeyEntity.PK != null && string.CompareOrdinal(fk.PKey, keyInfo.KeyEntity.PK.N) == 0;

                    if(keyInfo.IsPK)
                        keyInfo.Key = keyInfo.KeyEntity.PK;
                    else
                        keyInfo.Key = keyInfo.KeyEntity.UKs.GetByName(fk.PKey);

                    Helpers.ComposeKeyTypes(keyInfo);

                    result.Add(keyInfo);
                }

                return result;
            }
            private static void GenerateComposeDictionaryByKeyMethod(KeyInfo keyInfo, UsingNamespace entityNamespace, string methodName, string comments)
            {
                var dict = $"{Common.Names.MultiKeyDictionary}<{keyInfo.KeyTypes}>";
                var entityName = keyInfo.Entity.N;
                var entityType = !keyInfo.Entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

                var method = new MethodDeclaration
                {
                    Name = methodName,
                    ReturnType = new TypeInfo(dict),
                    AccessModifier = AccessModifiers.Private,
                    IsStatic = true,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { Name = "entities", ParamType = $"List<{entityType}>" },
                      new MethodParameter { Name = "entByKey", ParamType = dict },
                    },
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Comment(comments);
                CSharpFile.If("entByKey?.Count > 0");
                CSharpFile.ReturnIndented("entByKey");

                if(keyInfo.Key.Ps.Count == 1)
                    CSharpFile.Line($"entByKey = new {dict}(entities.Count)");
                else
                    CSharpFile.Line($"entByKey = new {dict}()");

                CSharpFile.ForEach("p", "entities");
                CSharpFile.LineIndented($"entByKey.Add({keyInfo.KeyPropCalls}, p)");
                CSharpFile.Return("entByKey");
                CSharpFile.CloseBlock();
            }
            private static void GenerateComposeKeys(Entity entity, UsingNamespace entityNamespace)
            {
                var filterConditionEnum = Common.Names.FilterConditionsEnum;
                var queryFilter = Common.Names.QueryFilter;
                var containerName = Common.GeneratePropClassName(entity);
                var entityName = entity.N;
                var entityType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
                var entityNameLower = entityName.ToLower();
                var filterCollection = $"List<{Common.Names.IQueryFilter}>";
                var keys = RepoGenerator.Helpers.GenerateKeysForSelectSingle(entity, queryFilter, containerName, filterConditionEnum, entityNameLower);

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.ComposeKeys,
                    ReturnType = new TypeInfo(filterCollection),
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { ParamType = entityType, Name = entityNameLower },
                    },
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                if(keys.Length > 0)
                    CSharpFile.Return($"new {filterCollection}{{ {keys} }}");
                else
                    CSharpFile.Return($"null");
                CSharpFile.CloseBlock();
            }

            private static void GenerateGetChanges(Entity entity, UsingNamespace entityNamespace)
            {
                var entityUtils = Common.GenerateEntityUtilName(entity);
                var entityName = entity.N;
                var entityType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
                var filterCollection = $"List<{Common.Names.IQueryFilter}>";
                var getChanges = $"{entityUtils}.{EntityUtilsMethodNames.GetChanges}";

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.GetChanges,
                    ReturnType = new TypeInfo(filterCollection),
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { ParamType = entityType, Name = "original" },
                      new MethodParameter { ParamType = entityType, Name = "changed" },
                    },
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                //the utils give the entity's typed changes; the SQL engine reads plain filters
                CSharpFile.Line($"var changes = {getChanges}(original, changed)");
                CSharpFile.If("changes == null");
                CSharpFile.LineIndented("return null");
                CSharpFile.EmptyLine();
                CSharpFile.Line($"var result = new {filterCollection}(changes.Count)");
                CSharpFile.Line("result.AddRange(changes)");
                CSharpFile.Return("result");
                CSharpFile.CloseBlock();
            }
            private static void GenerateMerge(Entity entity, UsingNamespace entityNamespace)
            {
                var entityUtils = Common.GenerateEntityUtilName(entity);

                var entityName = entity.N;
                var entityType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";

                var merge = $"{entityUtils}.{EntityUtilsMethodNames.Merge}";

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.Merge,
                    ReturnType = TypeInfo.Void,
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { ParamType = entityType, Name = "source" },
                      new MethodParameter { ParamType = entityType, Name = "target" },
                    },
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                CSharpFile.Line($"{merge}(source, target)");
                CSharpFile.CloseBlock();

            }
            private static void GenerateInsertPredicate(Entity entity, UsingNamespace entityNamespace)
            {
                var filterConditionEnum = Common.Names.FilterConditionsEnum;
                var queryFilter = Common.Names.QueryFilter;
                var containerName = Common.GeneratePropClassName(entity);
                var entityName = entity.N;
                var entityType = !entity.FullyQualify ? entityName : $"{entityNamespace.Alias}.{entityName}";
                var entityNameLower = entityName.ToLower();
                var filterCollection = $"List<{Common.Names.IQueryFilter}>";

                var method = new MethodDeclaration
                {
                    Name = RepoMethodNames.ComposeInsertPredicate,
                    ReturnType = new TypeInfo(filterCollection),
                    AccessModifier = AccessModifiers.Protected,
                    InheritanceModifier = InheritanceModifiers.Override,
                    Parameters = new List<MethodParameter>
                    {
                      new MethodParameter { ParamType = entityType, Name = entityNameLower },
                    },
                };

                CSharpFile.DeclareMethod(method);
                CSharpFile.OpenBlock();
                var props = entity.Ps;
                var propCount = props.Count;
                var valuesToSet = string.Empty;
                var isFirstDone = false;
                for(var i = 0; i < propCount; i++)
                {
                    var p = props[i];
                    if(p.RO == YesOrNo.Yes)
                        continue;

                    if(isFirstDone)
                    {
                        valuesToSet += $", {queryFilter}.New({containerName}.{p.N}, {filterConditionEnum}.Equals, {entityNameLower}.{p.N})";//do the rest with comma prefixed              
                    }
                    else
                    {
                        valuesToSet += $"{queryFilter}.New({containerName}.{p.N}, {filterConditionEnum}.Equals, {entityNameLower}.{p.N})";//do the first one to avoid trimming the last comma 
                        isFirstDone = true;
                    }
                }

                if(valuesToSet.Length > 0)
                    CSharpFile.Return($"new {filterCollection}{{ {valuesToSet} }}");
                else
                    CSharpFile.Return($"null");
                CSharpFile.CloseBlock();

            }
            private static class Helpers
            {
                public static void GenerateSwitchForAssociateNavPropResult(CommonSettings commonSettings, Model model, List<KeyInfo> keyInfos, Dictionary<string, string> ukSuffixByName, string navPropEnum)
                {
                    foreach(var ki in keyInfos)
                    {
                        CSharpFile.Case($"{navPropEnum}.{ki.NavProp.N}");
                        CSharpFile.OpenRegion(ki.NavProp.N);
                        CSharpFile.OpenBlock();
                        GenerateCaseBodyForSwitchForAssociateNavPropResult(commonSettings, model, ki, ukSuffixByName);
                        CSharpFile.CloseBlock();
                        CSharpFile.CloseRegion();
                        CSharpFile.Break();
                    }

                    CSharpFile.CaseDefault();
                    CSharpFile.ThrowArgumentException(_invalidNavPropExceptionMsg);
                }
                public static void ComposeKeyTypes(KeyInfo keyInfo)
                {
                    if(!(keyInfo.KeyEntity.Ps?.Count > 0))
                        return;

                    var key = keyInfo.Key;
                    var props = keyInfo.KeyEntity.Ps;
                    var count = key.Ps.Count;
                    var keyTypes = props.GetByName(key.Ps[0].N).T;
                    var keyProps = "p." + props.GetByName(key.Ps[0].N).N;

                    for(var i = 1; i < count; i++)
                    {
                        var p = props.GetByName(key.Ps[i].N);
                        keyTypes += ", " + p.T;
                        keyProps += ", p." + p.N;
                    }

                    keyInfo.KeyTypes = keyTypes + ", " + keyInfo.KeyEntityFullName;

                    keyInfo.KeyPropCalls = keyProps;
                }
                private static void GenerateCaseBodyForSwitchForAssociateNavPropResult(CommonSettings commonSettings, Model model, KeyInfo keyInfo, Dictionary<string, string> ukSuffixByName)
                {
                    CSharpFile.Line($"var list = {RepoMethodNames.UnboxEntityList}<{keyInfo.OtherEntityFullName}>(result)");

                    CSharpFile.If($"list?.Count > 0");
                    CSharpFile.OpenBlock();

                    var navPropNamespace = Common.GenerateTypeNamespace(commonSettings, model.Layers.Repository, model, keyInfo.OtherSchema);
                    var repoName = Common.GenerateRepositoryName(keyInfo.OtherEntity);

                    var fk = keyInfo.FK;
                    var parts = fk.FKParts;
                    var partCount = fk.FKParts.Count;

                    var searchDic = string.Empty;
                    var contains = string.Empty;
                    var arePropsNull = string.Empty;

                    if(partCount > 0)
                    {
                        for(var i = 0; i < partCount; i++)
                        {
                            var childProp = keyInfo.IsNavPropTheParent ? keyInfo.Entity.Ps.GetByName(parts[i].CCol) : keyInfo.OtherEntity.Ps.GetByName(parts[i].CCol);

                            var separator = i > 0 ? ", " : "";

                            if(childProp.Nul == YesOrNo.Yes)
                            {
                                if(!childProp.Str)
                                {
                                    searchDic += $"[c.{childProp.N}.Value]";
                                    contains += $"{separator}c.{childProp.N}.Value";
                                    arePropsNull += (arePropsNull.Length > 0 ? " || " : "") + $"!c.{childProp.N}.HasValue";
                                }
                                else
                                {
                                    searchDic += $"[c.{childProp.N}]";
                                    contains += $"{separator}c.{childProp.N}";
                                    arePropsNull += (arePropsNull.Length > 0 ? " || " : "") + $"string.IsNullOrWhiteSpace(c.{childProp.N})";
                                }
                            }
                            else
                            {
                                searchDic += $"[c.{childProp.N}]";
                                contains += $"{separator}c.{childProp.N}";
                            }
                        }
                    }

                    if(keyInfo.IsNavPropTheParent)
                    {
                        //this means the entity is the child therefore the nav property is a single rather than list
                        var parentNavPropName = keyInfo.NavProp.N;
                        CSharpFile.Line($"var navByKey = new {Common.Names.MultiKeyDictionary}<{keyInfo.KeyTypes}>()");
                        CSharpFile.ForEach("p", "list");
                        CSharpFile.LineIndented($"navByKey.Add({keyInfo.KeyPropCalls}, p)");
                        CSharpFile.ForEach("c", "entities");
                        CSharpFile.OpenBlock();
                        if(arePropsNull.Length > 0)
                        {
                            CSharpFile.If($"{arePropsNull}");//if any of the properties involved in the FK is null then skip the check
                            CSharpFile.LineIndented("continue");
                        }

                        CSharpFile.If($"navByKey.ContainsKey({contains})");
                        if(fk.PMu[0] == '1')
                        {
                            CSharpFile.LineIndented($"c.{parentNavPropName} = navByKey{searchDic}");
                        }
                        else
                        {// in theory this should never happen because if the nav property is the parent then it's multiplicity has to be one.
                            CSharpFile.OpenBlock();
                            CSharpFile.Line($"var p = navByKey{searchDic}");
                            CSharpFile.Line($"c.{parentNavPropName} = {RepoMethodNames.AddEntityToList}<{keyInfo.Entity.N}>(c.{parentNavPropName}, p)");
                            CSharpFile.CloseBlock();
                        }

                        CSharpFile.CloseBlock();
                    }
                    else
                    {
                        var childNavPropName = keyInfo.NavProp.N;
                        var key = keyInfo.Key;
                        if(keyInfo.IsPK)
                            CSharpFile.Line($"entByPK = {RepoMethodNames.ComposeDictionaryByPK}(entities, entByPK)");
                        else
                            CSharpFile.Line($"entByUK{ukSuffixByName[key.N]} = {RepoMethodNames.ComposeDictionaryByUK}{ukSuffixByName[key.N]}(entities, entByUK{ukSuffixByName[key.N]})");

                        CSharpFile.ForEach("c", "list");
                        CSharpFile.OpenBlock();

                        if(keyInfo.IsPK)
                            CSharpFile.Line($"var p = entByPK{searchDic}");
                        else
                            CSharpFile.Line($"var p = entByUK{ukSuffixByName[key.N]}{searchDic}");

                        if(fk.CMu[0] == '*')
                            CSharpFile.Line($"p.{childNavPropName} = {RepoMethodNames.AddEntityToList}<{keyInfo.OtherEntityFullName}>(p.{childNavPropName}, c)");
                        else
                            CSharpFile.Line($"p.{childNavPropName} = c");

                        CSharpFile.CloseBlock();
                    }

                    CSharpFile.CloseBlock();
                    CSharpFile.If("navprop.NavProps?.Count > 0");
                    CSharpFile.Line($"resultsetindex = {navPropNamespace}.{repoName}.{RepoMethodNames.PopulateNavProperties}(list, results, navprop.NavProps, resultsetindex)");
                }
            }
        }
    }
}

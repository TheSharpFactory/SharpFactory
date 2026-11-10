#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static class RepoInterfaceGenerator
{
    #region Entity And View Interfaces
    public static string Generate(ModelContainer modelContainer, Model model, Entity entity, Schema schema, bool isReadOnly)
    {
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

        var hasNavProps = entity.NPs?.Count > 0;
        var typeName = Common.GenerateIRepositoryName(entity);
        var typeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schema);
        var entityNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Entity, model, schema);
        var propEnumNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.PropEnum, model, schema);
        var navPropEnumNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.NavPropEnum, model, schema);
        var filterNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
        //var entityName = Common.GenerateTypeName(entity);
        var entityFullName = Common.GenerateTypeFullName(modelContainer.CommonSettings, model.Layers.Entity, model, schema, Common.GenerateTypeName(entity));

        var usingNamespaces = new List<UsingNamespace>(8)
        {
          new UsingNamespace { Name = "System" },
          new UsingNamespace { Name = "System.Collections.Generic" },
          new UsingNamespace { Name = "System.Threading.Tasks" },
          new UsingNamespace { Name = filterNamespace },
          new UsingNamespace { Name = propEnumNamespace },
        };

        if(hasNavProps && string.CompareOrdinal(propEnumNamespace, navPropEnumNamespace) != 0)
            usingNamespaces.Add(new UsingNamespace { Name = navPropEnumNamespace });

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

        var summary = $"This interface represents the contract to exchange data with a Repository for a {entity.N}.";
        //Generate file header with information about the interface.
        var type = new InterfaceDeclaration { Name = typeName, IsPartial = true, Namespace = typeNamespace, Summary = new List<string>(1) { summary } };
        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenInterfaceWithNamespace(type);
        Methods.Generate(entity, entNamespace, entityFullName, hasNavProps, isReadOnly);
        CSharpFile.CloseInterfaceWithNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }
    private static class Methods
    {
        public static void Generate(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool hasNavProps, bool isReadOnly)
        {
            //Retrieve
            CSharpFile.OpenRegion("Get");
            GenerateGetById(entity, entityNamespace, entityFullName, hasNavProps);
            GenerateGetByPK(entity, entityNamespace, entityFullName, hasNavProps);
            GenerateGetByUK(entity, entityNamespace, entityFullName, hasNavProps);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion(RepoMethodNames.GetTop);
            GenerateGetTop(entity, entityNamespace, entityFullName);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion(RepoMethodNames.GetFirstOrDefault);
            GenerateGetFirstOrDefault(entity, entityNamespace, entityFullName);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion(RepoMethodNames.GetFirst);
            GenerateGetFirst(entity, entityNamespace, entityFullName);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion(RepoMethodNames.GetSingleOrDefault);
            GenerateGetSingleOrDefault(entity, entityNamespace, entityFullName);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion(RepoMethodNames.GetSingle);
            GenerateGetSingle(entity, entityNamespace, entityFullName);
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion(RepoMethodNames.GetList);
            GenerateGetList(entity, entityNamespace, entityFullName);
            GenerateGetAll(entity, entityNamespace, entityFullName, hasNavProps);
            CSharpFile.CloseRegion();

            if(!isReadOnly)
            {
                //Create
                CSharpFile.OpenRegion("Create");
                GenerateCreate(entity, entityNamespace, entityFullName);
                GenerateCreateList(entity, entityNamespace, entityFullName);
                CSharpFile.CloseRegion();

                //Update
                CSharpFile.OpenRegion("Update");
                GenerateUpdate(entity, entityNamespace, entityFullName);
                GenerateUpdateAnyBasedOnQuery(entity, entityNamespace);
                GenerateUpdateOptimistic(entity, entityNamespace, entityFullName);
                GenerateUpdateOrCreate(entity, entityNamespace, entityFullName);
                GenerateUpdateList(entity, entityNamespace, entityFullName);
                GenerateUpdateOrCreateList(entity, entityNamespace, entityFullName);
                CSharpFile.CloseRegion();

                //Delete
                CSharpFile.OpenRegion("Delete");
                GenerateDeleteById(entity, entityFullName);
                GenerateDeleteByPK(entity, entityFullName);
                GenerateDeleteByUK(entity, entityFullName);
                GenerateDeleteEntity(entity, entityNamespace, entityFullName);
                GenerateDeleteAnyBasedOnQuery(entity);
                GenerateDeleteList(entity, entityNamespace, entityFullName);
                CSharpFile.CloseRegion();
            }
        }

        #region Create
        private static void GenerateCreate(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.CreateMethods.GenerateCreate(entity, entityNamespace, false, entityFullName, true, false);
            RepoMethodSignatures.CreateMethods.GenerateCreate(entity, entityNamespace, true, entityFullName, true, false);
        }

        private static void GenerateCreateList(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.CreateMethods.GenerateCreateList(entity, entityNamespace, false, entityFullName, true);
            RepoMethodSignatures.CreateMethods.GenerateCreateList(entity, entityNamespace, true, entityFullName, true);
        }

        #endregion

        #region Retrieve

        #region Get
        private static void GenerateGetById(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool hasNavProps)
        {
            if(entity.HId == YesOrNo.No)
                return;//if there is no identity then there is nothing to generate

            if(entity.PK?.Ps?.Count == 1)
            {
                for(var i = 0; i < entity.Ps.Count; i++)
                {
                    if(entity.Ps[i].Ide == YesOrNo.Yes)
                    {
                        if(string.CompareOrdinal(entity.Ps[i].N, entity.PK.Ps[0].N) == 0)
                            return;//if the PK and the Identity are the same there is no need to have two methods to do the same thing
                    }
                }
            }

            RepoMethodSignatures.RetrieveMethods.GenerateGetById(entity, entityNamespace, false, entityFullName, true, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGetById(entity, entityNamespace, true, entityFullName, true, false);

            if(hasNavProps)//if it has navprops then create 2 overloads. one with nav props and one without.
            {
                RepoMethodSignatures.RetrieveMethods.GenerateGetById(entity, entityNamespace, false, entityFullName, true, true);
                RepoMethodSignatures.RetrieveMethods.GenerateGetById(entity, entityNamespace, true, entityFullName, true, true);
            }
        }
        private static void GenerateGetByPK(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool hasNavProps)
        {
            if(entity.PK == null || entity.PK.Ps.Count < 1)
                return;//if there is no PrimaryKey then there is nothing to generate

            RepoMethodSignatures.RetrieveMethods.GenerateGetByPK(entity, entityNamespace, false, entityFullName, true, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGetByPK(entity, entityNamespace, true, entityFullName, true, false);

            if(hasNavProps)//if it has navprops then create 2 overloads. one with nav props and one without.
            {
                RepoMethodSignatures.RetrieveMethods.GenerateGetByPK(entity, entityNamespace, false, entityFullName, true, true);
                RepoMethodSignatures.RetrieveMethods.GenerateGetByPK(entity, entityNamespace, true, entityFullName, true, true);
            }
        }
        private static void GenerateGetByUK(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool hasNavProps)
        {
            if(entity.UKs == null || entity.UKs.Count < 1)
                return;//if there is no Unique Keys then there is nothing to generate

            var keyCount = 0;
            var hasSingleKey = entity.UKs.Count == 1;
            foreach(var uk in entity.UKs)
            {
                if(uk.Ps == null || uk.Ps.Count < 1)
                    continue;

                keyCount++;//this is the suffix to differentiate the method names.

                var keySuffix = hasSingleKey ? "" : keyCount.ToString();
                RepoMethodSignatures.RetrieveMethods.GenerateGetByUK(entity, entityNamespace, false, uk, entityFullName, keySuffix, true, false);
                RepoMethodSignatures.RetrieveMethods.GenerateGetByUK(entity, entityNamespace, true, uk, entityFullName, keySuffix, true, false);

                if(hasNavProps)//if it has navprops then create 2 overloads. one with nav props and one without.
                {
                    RepoMethodSignatures.RetrieveMethods.GenerateGetByUK(entity, entityNamespace, false, uk, entityFullName, keySuffix, true, true);
                    RepoMethodSignatures.RetrieveMethods.GenerateGetByUK(entity, entityNamespace, true, uk, entityFullName, keySuffix, true, true);
                }
            }
        }
        #endregion

        #region GetTop
        private static void GenerateGetTop(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            //NO NAV PROPERTIES, NO QUERY
            RepoMethodSignatures.RetrieveMethods.GenerateGenericGetTopNoQuery(entity, false, true, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGenericGetTopNoQuery(entity, true, true, false);

            //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)
            RepoMethodSignatures.RetrieveMethods.GenerateGetTopBasedOnQuery(entity, entityNamespace, entityFullName, false, true, false, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGetTopBasedOnQuery(entity, entityNamespace, entityFullName, true, true, false, false);
        }
        #endregion

        #region GetFirstOrDefault
        private static void GenerateGetFirstOrDefault(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)
            RepoMethodSignatures.RetrieveMethods.GenerateGetFirstOrDefaultBasedOnQuery(entity, entityNamespace, entityFullName, false, true, false, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGetFirstOrDefaultBasedOnQuery(entity, entityNamespace, entityFullName, true, true, false, false);
        }

        #endregion

        #region GetFirst
        private static void GenerateGetFirst(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)
            RepoMethodSignatures.RetrieveMethods.GenerateGetFirstBasedOnQuery(entity, entityNamespace, entityFullName, false, true, false, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGetFirstBasedOnQuery(entity, entityNamespace, entityFullName, true, true, false, false);
        }
        #endregion

        #region GetSingleOrDefault
        private static void GenerateGetSingleOrDefault(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)
            RepoMethodSignatures.RetrieveMethods.GenerateGetSingleOrDefaultBasedOnQuery(entity, entityNamespace, entityFullName, false, true, false, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGetSingleOrDefaultBasedOnQuery(entity, entityNamespace, entityFullName, true, true, false, false);
        }

        #endregion

        #region GetSingle
        private static void GenerateGetSingle(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)
            RepoMethodSignatures.RetrieveMethods.GenerateGetSingleBasedOnQuery(entity, entityNamespace, entityFullName, false, true, false, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGetSingleBasedOnQuery(entity, entityNamespace, entityFullName, true, true, false, false);
        }
        #endregion

        #region GetList
        private static void GenerateGetList(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            //BASED ON QUERY: NAV PROPERTIES COME FROM THE QUERY (SelectQuery)
            RepoMethodSignatures.RetrieveMethods.GenerateGetListBasedOnQuery(entity, entityNamespace, entityFullName, false, true, false, false);
            RepoMethodSignatures.RetrieveMethods.GenerateGetListBasedOnQuery(entity, entityNamespace, entityFullName, true, true, false, false);

            //NO NAV PROPERTIES, DEFAULT SORTING
            //RepoMethodSignatures.RetrieveMethods.GenerateGetList(entity, entityName, entityFullName, true, false, false, true, false);//no paging
            //RepoMethodSignatures.RetrieveMethods.GenerateGetList(entity, entityName, entityFullName, true, false, false, true, true);//with paging

            //NO NAV PROPERTIES, WITH SORTING
            //RepoMethodSignatures.RetrieveMethods.GenerateGetList(entity, entityName, entityFullName, true, false, false, false, false);//no paging
            //RepoMethodSignatures.RetrieveMethods.GenerateGetList(entity, entityName, entityFullName, true, false, false, false, true);//with paging
        }
        #endregion

        #region GetAll
        private static void GenerateGetAll(Entity entity, UsingNamespace entityNamespace, string entityFullName, bool hasNavProps)
        {
            //NO NAV PROPERTIES, DEFAULT SORTING
            RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityNamespace, false, entityFullName, true, false, false, true, false);//no paging
            RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityNamespace, true, entityFullName, true, false, false, true, false);//no pag
                                                                                                                                                //RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityName, entityFullName, true, false, false, true, true);//with paging

            //NO NAV PROPERTIES, WITH SORTING
            //RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityName, entityFullName, true, false, false, false, false);//no paging
            //RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityName, entityFullName, true, false, false, false, true);//with paging

            if(hasNavProps)
            {//WITH NAV PROPS
             //DEFAULT SORTING
             //RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityName, entityFullName, true, true, false, true, false);//no paging
             //RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityName, entityFullName, true, true, false, true, true);//with paging

                //WITH SORTING
                //RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityName, entityFullName, true, true, false, false, false);//no paging
                //RepoMethodSignatures.RetrieveMethods.GenerateGetAll(entity, entityName, entityFullName, true, true, false, false, true);//with paging
            }
        }
        #endregion

        #endregion

        #region Update
        private static void GenerateUpdate(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.UpdateMethods.GenerateUpdate(entity, entityNamespace, false, entityFullName, true, false);
            RepoMethodSignatures.UpdateMethods.GenerateUpdate(entity, entityNamespace, true, entityFullName, true, false);
        }
        private static void GenerateUpdateAnyBasedOnQuery(Entity entity, UsingNamespace entityNamespace)
        {
            RepoMethodSignatures.UpdateMethods.GenerateUpdateAnyBasedOnQuery(entity, entityNamespace, false, true, false);
            RepoMethodSignatures.UpdateMethods.GenerateUpdateAnyBasedOnQuery(entity, entityNamespace, true, true, false);
        }
        private static void GenerateUpdateOptimistic(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.UpdateMethods.GenerateUpdateOptimistic(entity, entityNamespace, false, entityFullName, true, false);
            RepoMethodSignatures.UpdateMethods.GenerateUpdateOptimistic(entity, entityNamespace, true, entityFullName, true, false);

            RepoMethodSignatures.UpdateMethods.GenerateUpdateOptimisticBatch(entity, entityNamespace, false, entityFullName, true, false);
            RepoMethodSignatures.UpdateMethods.GenerateUpdateOptimisticBatch(entity, entityNamespace, true, entityFullName, true, false);
        }
        private static void GenerateUpdateOrCreate(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.UpdateMethods.GenerateUpdateOrCreate(entity, entityNamespace, false, entityFullName, true, false);
            RepoMethodSignatures.UpdateMethods.GenerateUpdateOrCreate(entity, entityNamespace, true, entityFullName, true, false);
        }
        private static void GenerateUpdateList(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.UpdateMethods.GenerateUpdateList(entity, entityNamespace, false, entityFullName, true);
            RepoMethodSignatures.UpdateMethods.GenerateUpdateList(entity, entityNamespace, true, entityFullName, true);
        }
        private static void GenerateUpdateOrCreateList(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.UpdateMethods.GenerateUpdateOrCreateList(entity, entityNamespace, false, entityFullName, true);
            RepoMethodSignatures.UpdateMethods.GenerateUpdateOrCreateList(entity, entityNamespace, true, entityFullName, true);
        }

        #endregion

        #region Delete
        private static void GenerateDeleteById(Entity entity, string entityFullName)
        {
            if(entity.HId == YesOrNo.No)
                return;//if there is no identity then there is nothing to generate

            if(entity.PK?.Ps?.Count == 1)
            {
                for(var i = 0; i < entity.Ps.Count; i++)
                {
                    if(entity.Ps[i].Ide == YesOrNo.Yes)
                    {
                        if(string.CompareOrdinal(entity.Ps[i].N, entity.PK.Ps[0].N) == 0)
                            return;//if the PK and the Identity are the same there is no need to have two methods to do the same thing
                    }
                }
            }

            RepoMethodSignatures.DeleteMethods.GenerateDeleteById(entity, false, entityFullName, true);
            RepoMethodSignatures.DeleteMethods.GenerateDeleteById(entity, true, entityFullName, true);
        }
        private static void GenerateDeleteByPK(Entity entity, string entityFullName)
        {
            if(entity.PK == null || entity.PK.Ps.Count < 1)
                return;//if there is no PrimaryKey then there is nothing to generate

            RepoMethodSignatures.DeleteMethods.GenerateDeleteByPK(entity, false, entityFullName, true);
            RepoMethodSignatures.DeleteMethods.GenerateDeleteByPK(entity, true, entityFullName, true);
        }
        private static void GenerateDeleteByUK(Entity entity, string entityFullName)
        {
            if(entity.UKs == null || entity.UKs.Count < 1)
                return;//if there is no Unique Keys then there is nothing to generate

            var keyCount = 0;
            var hasSingleKey = entity.UKs.Count == 1;
            foreach(var uk in entity.UKs)
            {
                if(uk.Ps == null || uk.Ps.Count < 1)
                    continue;

                keyCount++;//this is the suffix to differentiate the method names.

                var keySuffix = hasSingleKey ? "" : keyCount.ToString();
                RepoMethodSignatures.DeleteMethods.GenerateDeleteByUK(entity, uk, false, entityFullName, keySuffix, true);
                RepoMethodSignatures.DeleteMethods.GenerateDeleteByUK(entity, uk, true, entityFullName, keySuffix, true);
            }
        }
        private static void GenerateDeleteEntity(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.DeleteMethods.GenerateDeleteByEntity(entity, entityNamespace, false, entityFullName, true, false);
            RepoMethodSignatures.DeleteMethods.GenerateDeleteByEntity(entity, entityNamespace, true, entityFullName, true, false);
        }
        private static void GenerateDeleteAnyBasedOnQuery(Entity entity)
        {
            RepoMethodSignatures.DeleteMethods.GenerateDeleteAnyBasedOnQuery(entity, false, true, false);
            RepoMethodSignatures.DeleteMethods.GenerateDeleteAnyBasedOnQuery(entity, true, true, false);
        }
        private static void GenerateDeleteList(Entity entity, UsingNamespace entityNamespace, string entityFullName)
        {
            RepoMethodSignatures.DeleteMethods.GenerateDeleteList(entity, entityNamespace, false, entityFullName, true);
            RepoMethodSignatures.DeleteMethods.GenerateDeleteList(entity, entityNamespace, true, entityFullName, true);
        }
        #endregion
    }
    #endregion

    #region StoredProc Interfaces
    public static string GenerateStoredProcs(ModelContainer modelContainer, Model model, Schema schema)
    {//stored procedure repositories are by schema. single file contains all stored procedures for that schema.
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
        var typeName = Common.Names.IStoredProcRepository;
        var typeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schema);
        //var typeDeclaration = $"public partial interface {typeName}";
        var entityTypeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Entity, model, schema);
        var filterNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);

        var usingNamespaces = new List<UsingNamespace>(4)
        {
          new UsingNamespace { Name = "System" },
          new UsingNamespace { Name = "System.Collections.Generic" },
          new UsingNamespace { Name = "System.Threading.Tasks" },
          new UsingNamespace { Name = entityTypeNamespace },
          new UsingNamespace { Name = filterNamespace }
        };

        var summary = $"This interface represents the Repository for all Stored Procedures under the {schema.Name} schema.";

        var type = new InterfaceDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            Summary = new List<string> { summary }
        };

        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenInterfaceWithNamespace(type);

        foreach(var proc in schema.StoredProcs)
        {
            RepoMethodSignatures.StoredProcs.Generate(proc, false, true, false);
            RepoMethodSignatures.StoredProcs.Generate(proc, true, true, false);
        }

        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();
        return CSharpFile.ToString();
    }
    #endregion

    #region Sequences Interfaces
    public static string GenerateSequences(ModelContainer modelContainer, Model model, Schema schema)
    {//stored procedure repositories are by schema. single file contains all stored procedures for that schema.
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
        var typeName = Common.Names.ISequenceRepository;
        var typeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schema);
        // var typeDeclaration = $"public partial interface {typeName}";

        var usingNamespaces = new List<UsingNamespace>(4)
        {
          new UsingNamespace { Name = "System" },
          new UsingNamespace { Name = "System.Collections.Generic" },
          new UsingNamespace { Name = "System.Threading.Tasks" },
          //new UsingNamespace { Name = entityTypeNamespace },
          //new UsingNamespace { Name = filterNamespace }
        };
        var summary = $"This interface represents the Repository for all Sequences under the {schema.Name} schema.";

        var type = new InterfaceDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            Summary = new List<string> { summary }
        };

        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenInterfaceWithNamespace(type);

        foreach(var seq in schema.Sequences)
        {
            RepoMethodSignatures.Sequences.Generate(seq, false, true, false);
            RepoMethodSignatures.Sequences.Generate(seq, true, true, false);
        }

        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();
        return CSharpFile.ToString();
    }
    #endregion

    #region Function Interfaces
    public static string GenerateFunctions(ModelContainer modelContainer, Model model, Schema schema)
    {//function repositories are by schema. single file contains all functions for that schema.
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
        var typeName = Common.Names.IFunctionRepository;
        var typeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schema);
        //var typeDeclaration = $"public partial interface {typeName}";
        var entityTypeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Entity, model, schema);

        var usingNamespaces = new List<UsingNamespace>(4)
        {
          new UsingNamespace { Name = "System" },
          new UsingNamespace { Name = "System.Collections.Generic" },
          new UsingNamespace { Name = "System.Threading.Tasks" },
          new UsingNamespace { Name = entityTypeNamespace }
        };

        var summary = $"This interface represents the Repository for all Functions under the {schema.Name} schema.";

        var type = new InterfaceDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            Summary = new List<string> { summary }
        };

        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenInterfaceWithNamespace(type);

        foreach(var func in schema.Functions)
        {
            RepoMethodSignatures.Functions.Generate(func, false, true, false);
            RepoMethodSignatures.Functions.Generate(func, true, true, false);
        }

        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();
        return CSharpFile.ToString();
    }
    #endregion
}


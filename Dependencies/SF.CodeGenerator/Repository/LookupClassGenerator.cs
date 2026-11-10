#region Usings
using System;
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    public static string GenerateLookupClass(ModelContainer modelContainer)
    {
        return LookupClass.Generate(modelContainer);
    }

    private static class LookupClass
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);

            var typeName = Common.Names.RepositoryLookup;
            var typeNamespace = modelContainer.CommonLayers.Repository.FullNamespace(modelContainer.CommonSettings);
            //var typeDeclaration = $"public static partial class {typeName}";

            var usingNamespaces = new List<UsingNamespace>(8)
            {
              new UsingNamespace { Name = "System" },
              new UsingNamespace { Name = "System.Data" },
              new UsingNamespace { Name = "System.Collections.Generic" }
            };

            if(modelContainer.HasEntities || modelContainer.HasViews || modelContainer.HasStoredProcEntities)
            {
                foreach(var m in modelContainer.Models)
                {
                    var modelNamespace = new UsingNamespace
                    {
                        Alias = m.Name + "E",
                        AliasRequired = true,
                        Name = m.Layers.PropEnum.FullNamespace(modelContainer.CommonSettings) + "." + m.Name,
                    };
                    usingNamespaces.Add(modelNamespace);
                }
            }

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                IsStatic = true,
                IsPartial = true,
                Summary = new List<string> { "This class contains constants and infrastructure information for the functioning of the Repository classes and Repository Base Classes." }
            };

            //Generate file header with information about the interface.
            CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            // Generate the Model Id constants
            typeName = Common.Names.RepositoryLookupModelId;
            type.Name = typeName;
            type.IsPartial = false;
            type.Summary[0] = "This class contains Model Id's.";

            CSharpFile.OpenRegion(typeName);
            CSharpFile.OpenClass(type);
            GenerateModelIdClassBody(modelContainer);//generate all the id fields
            CSharpFile.CloseType();
            CSharpFile.CloseRegion();

            // Generate the Entity Id constants
            typeName = Common.Names.RepositoryLookupEntityId;
            type.Name = typeName;
            type.Summary[0] = "This class contains Entity Id's.";

            CSharpFile.OpenRegion(typeName);
            CSharpFile.OpenClass(type);
            var entityCount = GenerateModelObjectIdClassBodyEntitiesAndViews(modelContainer);//generate all the id fields
            CSharpFile.CloseType();
            CSharpFile.CloseRegion();

            // Generate the Stored Proc Id constants
            typeName = Common.Names.RepositoryLookupStoredProcId;
            type.Name = typeName;
            type.Summary[0] = "This class contains StoredProc Id's.";

            CSharpFile.OpenRegion(typeName);
            CSharpFile.OpenClass(type);
            var procCount = GenerateModelObjectIdClassBodyStoredProcs(modelContainer);//generate all the id fields
            CSharpFile.CloseType();
            CSharpFile.CloseRegion();

            // Generate the Function Id constants
            typeName = Common.Names.RepositoryLookupFunctionId;
            type.Name = typeName;
            type.Summary[0] = "This class contains Function Id's.";

            CSharpFile.OpenRegion(typeName);
            CSharpFile.OpenClass(type);
            var funcCount = GenerateModelObjectIdClassBodyFuncs(modelContainer);//generate all the id fields
            CSharpFile.CloseType();
            CSharpFile.CloseRegion();

            // Generate the Sequence Id constants
            typeName = Common.Names.RepositoryLookupSequenceId;
            type.Name = typeName;
            type.Summary[0] = "This class contains Sequence Id's.";

            CSharpFile.OpenRegion(typeName);
            CSharpFile.OpenClass(type);
            var seqCount = GenerateModelObjectIdClassBodySequences(modelContainer);//generate all the id fields
            CSharpFile.CloseType();
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Entities And Views");
            GenerateEntityInfoDictionary(modelContainer, entityCount);//generate all the entityinfo
            CSharpFile.CloseRegion();

            //CSharpFile.OpenRegion("Views");
            //GenerateViewEntityInfoDictionary(modelContainer, entityCount);//generate all the entityinfo
            //CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("StoredProcs");
            GenerateStoredProcEntityInfoDictionary(modelContainer, procCount);//generate all the entityinfo
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Functions");
            GenerateFunctionEntityInfoDictionary(modelContainer, funcCount);//generate all the entityinfo
            CSharpFile.CloseRegion();

            CSharpFile.OpenRegion("Sequences");
            GenerateSequenceEntityInfoDictionary(modelContainer, seqCount);//generate all the entityinfo
            CSharpFile.CloseRegion();

            CSharpFile.CloseTypeWithNamespace();
            CSharpFile.Footer();

            return CSharpFile.ToString();
        }

        private static int GenerateModelObjectIdClassBodyEntitiesAndViews(ModelContainer modelContainer)
        {
            //first generate the int id's to make it superfast to look up        
            var id = 1;
            var count = 0;
            var models = modelContainer.Models;
            var modelCount = models.Count;

            var field = new FieldDeclaration
            {
                AccessModifier = AccessModifiers.Public,
                IsReadonly = true,
                IsStatic = true,
                ReturnType = TypeInfo.Int
            };

            for(var i = 0; i < modelCount; i++)
            {
                var model = models[i];
                var catEnt = model.Categories.GetByName(CategoryNames.Entities);
                var catView = model.Categories.GetByName(CategoryNames.Views);
                if(catEnt.Count < 1 && catView.Count < 1)
                    continue;

                count += catEnt.Count;//entity count
                count += catView.Count;//view count

                var modelName = model.Name;

                foreach(var s in catEnt.Schemas)
                {
                    if(s.Count < 1)
                        continue;
                    var schemaName = s.Name;

                    foreach(var e in s.Entities)
                    {
                        field.Name = Common.GenerateRepositoryLookupModelObjectIdsFieldName(modelName, schemaName, e.N);
                        field.DefaultValue = id.ToString();

                        CSharpFile.Field(field);

                        id++;
                    }
                }

                foreach(var s in catView.Schemas)
                {
                    if(s.Count < 1)
                        continue;
                    var schemaName = s.Name;

                    foreach(var e in s.Views)
                    {
                        field.Name = Common.GenerateRepositoryLookupModelObjectIdsFieldName(modelName, schemaName, e.N);
                        field.DefaultValue = id.ToString();

                        CSharpFile.Field(field);

                        id++;
                    }
                }
            }

            return count;
        }
        private static int GenerateModelObjectIdClassBodyFuncs(ModelContainer modelContainer)
        {
            //first generate the int id's to make it superfast to look up        
            var id = 1;
            var count = 0;
            var models = modelContainer.Models;
            var modelCount = models.Count;

            var field = new FieldDeclaration
            {
                AccessModifier = AccessModifiers.Public,
                IsReadonly = true,
                IsStatic = true,
                ReturnType = TypeInfo.Int
            };

            for(var i = 0; i < modelCount; i++)
            {
                var model = models[i];
                var cat = model.Categories.GetByName(CategoryNames.Funcs);
                if(cat.Count < 1)
                    continue;

                count += cat.Count;//function count

                var modelName = model.Name;
                foreach(var s in cat.Schemas)
                {
                    if(s.Count < 1)
                        continue;
                    var schemaName = s.Name;

                    foreach(var e in s.Functions)
                    {
                        field.Name = Common.GenerateRepositoryLookupModelObjectIdsFieldName(modelName, schemaName, e.N);
                        field.DefaultValue = id.ToString();

                        CSharpFile.Field(field);

                        id++;
                    }
                }
            }

            return count;
        }
        private static int GenerateModelObjectIdClassBodyStoredProcs(ModelContainer modelContainer)
        {
            //first generate the int id's to make it superfast to look up        
            var id = 1;
            var count = 0;
            var models = modelContainer.Models;
            var modelCount = models.Count;

            var field = new FieldDeclaration
            {
                AccessModifier = AccessModifiers.Public,
                IsReadonly = true,
                IsStatic = true,
                ReturnType = TypeInfo.Int
            };

            for(var i = 0; i < modelCount; i++)
            {
                var model = models[i];
                var cat = model.Categories.GetByName(CategoryNames.Procs);
                if(cat.Count < 1)
                    continue;

                count += cat.Count;//stored proc count

                var modelName = model.Name;
                foreach(var s in cat.Schemas)
                {
                    if(s.Count < 1)
                        continue;
                    var schemaName = s.Name;

                    foreach(var e in s.StoredProcs)
                    {
                        field.Name = Common.GenerateRepositoryLookupModelObjectIdsFieldName(modelName, schemaName, e.N);
                        field.DefaultValue = id.ToString();

                        CSharpFile.Field(field);

                        id++;
                    }
                }
            }

            return count;
        }
        private static int GenerateModelObjectIdClassBodySequences(ModelContainer modelContainer)
        {
            //first generate the int id's to make it superfast to look up        
            var id = 1;
            var count = 0;
            var models = modelContainer.Models;
            var modelCount = models.Count;

            var field = new FieldDeclaration
            {
                AccessModifier = AccessModifiers.Public,
                IsReadonly = true,
                IsStatic = true,
                ReturnType = TypeInfo.Int
            };

            for(var i = 0; i < modelCount; i++)
            {
                var model = models[i];
                var cat = model.Categories.GetByName(CategoryNames.Seqs);
                if(cat.Count < 1)
                    continue;

                count += cat.Count;//stored proc count

                var modelName = model.Name;
                foreach(var s in cat.Schemas)
                {
                    if(s.Count < 1)
                        continue;
                    var schemaName = s.Name;

                    foreach(var e in s.Sequences)
                    {
                        field.Name = Common.GenerateRepositoryLookupModelObjectIdsFieldName(modelName, schemaName, e.N);
                        field.DefaultValue = id.ToString();

                        CSharpFile.Field(field);

                        id++;
                    }
                }
            }

            return count;
        }

        private static void GenerateModelIdClassBody(ModelContainer modelContainer)
        {
            //first generate the int id's to make it superfast to look up

            var models = modelContainer.Models;
            var modelCount = models.Count;

            var field = new FieldDeclaration
            {
                AccessModifier = AccessModifiers.Public,
                IsReadonly = true,
                IsStatic = true,
                ReturnType = TypeInfo.Int
            };

            for(var i = 0; i < modelCount; i++)
            {
                field.Name = models[i].Name;
                field.DefaultValue = (i + 1).ToString();

                CSharpFile.Field(field);
            }
        }
        //private static void GenerateModelNameCode(ModelContainer modelContainer)
        //{
        //  //first generate the int id's to make it superfast to look up        
        //  var models = modelContainer.Models;
        //  var modelCount = models.Count;
        //  for(var i = 0; i < modelCount; i++)
        //    CSharpFile.Line($"internal static readonly string {models[i].Name} = \"{models[i].Name}\"");
        //}
        private static void GenerateEntityInfoDictionary(ModelContainer modelContainer, int entityCount)
        {
            //first generate the int id's to make it superfast to look up
            var entityInfoName = Common.Names.RepositoryEntityInfo;
            CSharpFile.Comment("this is the info necesary for Sql query composition. It is public so the repositories can interact with each other.");
            CSharpFile.LineNoEnding($"public static readonly Dictionary<int, {entityInfoName}> {Common.GenerateRepositoryLookupEntityInfosFieldName()} = new Dictionary<int, {entityInfoName}>({entityCount})");
            CSharpFile.OpenBlock();
            foreach(var model in modelContainer.Models)
            {
                GenerateEntityInfoDictionaryBody(model, ModelObjectType.Entities);
                GenerateEntityInfoDictionaryBody(model, ModelObjectType.Views);
            }

            CSharpFile.CloseBlockWithSemicolon();
        }

        private static void GenerateEntityInfoDictionaryBody(Model model, ModelObjectType objectType)
        {
            var catName = Common.GetCategoryName(objectType);
            var catEntities = model.Categories.GetByName(catName);

            if(catEntities.Count < 1)
                return;

            foreach(var s in catEntities.Schemas)
            {
                if(s.Count < 1)
                    continue;

                var ents = (EntityCollection)null;
                switch(objectType)
                {
                    case ModelObjectType.Entities:
                        ents = s.Entities;
                        break;
                    case ModelObjectType.Views:
                        ents = s.Views;
                        break;
                    default:
                        throw new ArgumentException($"GenerateEntityInfoDictionaryBody does not support Object Type {objectType}");
                }

                foreach(var e in ents)
                    EntityInfoClass.GenerateEntityInfoInstanceForRepositoryLookup(model, e, s);
            }
        }
        private static void GenerateStoredProcEntityInfoDictionary(ModelContainer modelContainer, int procCount)
        {
            //first generate the int id's to make it superfast to look up
            var entityInfoName = Common.Names.RepositoryEntityInfo;
            CSharpFile.Comment("this is the info necesary for Sql query composition. It is public so the repositories can interact with each other.");
            CSharpFile.LineNoEnding($"public static readonly Dictionary<int, {entityInfoName}> {Common.GenerateRepositoryLookupStoredProcInfosFieldName()} = new Dictionary<int, {entityInfoName}>({procCount})");
            CSharpFile.OpenBlock();
            foreach(var model in modelContainer.Models)
            {
                var cat = model.Categories.GetByName(CategoryNames.Procs);
                if(cat.Count < 1)
                    continue;

                foreach(var s in cat.Schemas)
                {
                    if(s.Count < 1)
                        continue;

                    foreach(var e in s.StoredProcs)
                        EntityInfoClass.GenerateStoredProcInfoInstanceForRepositoryLookup(model, e, s);
                }
            }

            CSharpFile.CloseBlockWithSemicolon();
        }
        private static void GenerateFunctionEntityInfoDictionary(ModelContainer modelContainer, int funcCount)
        {
            //first generate the int id's to make it superfast to look up

            var entityInfoName = Common.Names.RepositoryEntityInfo;
            CSharpFile.Comment("this is the info necesary for Sql query composition. It is public so the repositories can interact with each other.");
            CSharpFile.LineNoEnding($"public static readonly Dictionary<int, {entityInfoName}> {Common.GenerateRepositoryLookupFunctionInfosFieldName()} = new Dictionary<int, {entityInfoName}>({funcCount})");
            CSharpFile.OpenBlock();
            foreach(var model in modelContainer.Models)
            {
                var cat = model.Categories.GetByName(CategoryNames.Funcs);
                if(cat.Count < 1)
                    continue;

                foreach(var s in cat.Schemas)
                {
                    if(s.Count < 1)
                        continue;

                    foreach(var e in s.Functions)
                        EntityInfoClass.GenerateFunctionInfoInstanceForRepositoryLookup(model, e, s);
                }
            }

            CSharpFile.CloseBlockWithSemicolon();
        }
        private static void GenerateSequenceEntityInfoDictionary(ModelContainer modelContainer, int procCount)
        {
            //first generate the int id's to make it superfast to look up

            var entityInfoName = Common.Names.RepositoryEntityInfo;
            CSharpFile.Comment("this is the info necesary for Sql query composition. It is public so the repositories can interact with each other.");
            CSharpFile.LineNoEnding($"public static readonly Dictionary<int, {entityInfoName}> {Common.GenerateRepositoryLookupSequenceInfosFieldName()} = new Dictionary<int, {entityInfoName}>({procCount})");
            CSharpFile.OpenBlock();
            foreach(var model in modelContainer.Models)
            {
                var cat = model.Categories.GetByName(CategoryNames.Seqs);
                if(cat.Count < 1)
                    continue;

                foreach(var s in cat.Schemas)
                {
                    if(s.Count < 1)
                        continue;

                    foreach(var e in s.Sequences)
                        EntityInfoClass.GenerateSequenceInfoInstanceForRepositoryLookup(model, e, s);
                }
            }

            CSharpFile.CloseBlockWithSemicolon();
        }
    }
}

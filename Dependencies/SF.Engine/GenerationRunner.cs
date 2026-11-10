#region Usings
using SF.CodeGeneration;
using SF.Models;
using SF.Utils;
using System.Collections.Generic;
#endregion

namespace SF.Engine;

/// <summary>
/// The files and projects one generation run produced.
/// </summary>
public sealed class GenerationResult
{
    /// <summary>
    /// Every generated file, in generation order.
    /// </summary>
    public List<CodeFile> Files { get; set; } = new List<CodeFile>();

    /// <summary>
    /// The projects the run used, keyed by upper-case name, in the order they were first prepared.
    /// </summary>
    public Dictionary<string, string> Projects { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The folder of each project, keyed by upper-case name.
    /// </summary>
    public Dictionary<string, string> ProjectFolders { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Generates every file of a model container in the one order Sync and GenCheck share.
/// The order matters: generators draw ids from a shared counter, so a different order gives different files.
/// </summary>
public static class GenerationRunner
{
    public static GenerationResult Run(ModelContainer modelContainer, IProjectHost host, IProgressLog log)
    {
        modelContainer = SqlVariantFilter.Apply(modelContainer, log);

        var result = new GenerationResult
        {
            Files = new List<CodeFile>((modelContainer.ObjectCount * 6) + 50)
        };

        //the generator writes through a static delegate: point it at this host for the run only, so a finished host
        //(and its report) is not kept alive and later code does not write into it
        var previousWriter = CodeGenUtils.FileWriter;
        CodeGenUtils.FileWriter = host.WriteFile;
        try
        {
            GenerateAll(modelContainer, host, log, result);
        }
        finally
        {
            CodeGenUtils.FileWriter = previousWriter;
        }

        return result;
    }

    private static void GenerateAll(ModelContainer modelContainer, IProjectHost host, IProgressLog log, GenerationResult result)
    {
        var tracker = new ProjectTracker(host, result);
        var filesInModel = result.Files;
        var commonLayers = modelContainer.CommonLayers;
        var commonSettings = modelContainer.CommonSettings;

        CodeGenUtils.Reset(commonSettings);
        log.Info("Initialized Code Generator");

        //first do the common layers
        log.Info("Processing Common Layers....");
        log.Info("Processing Common Layer Project: Query");
        var cCommonFolder = tracker.PrepareProject(commonLayers.Query.FullProjectName(commonSettings), null);

        log.Info("Processing Common Layer Project: Exceptions");
        var cExceptionsFolder = tracker.PrepareProject(commonLayers.Exceptions.FullProjectName(commonSettings), null);

        log.Info("Processing Common Layer Project: Repository");
        var cRepositoryFolder = tracker.PrepareProject(commonLayers.Repository.FullProjectName(commonSettings), null);

        log.Info("Processing Common Layer Project: Repository Container Interface");
        var cIRepoContainerFolder = tracker.PrepareProject(commonLayers.IRepoContainer.FullProjectName(commonSettings), null);

        log.Info("Processing Common Layer Project: Repository Container");
        var cRepoContainerFolder = tracker.PrepareProject(commonLayers.RepoContainer.FullProjectName(commonSettings), null);

        log.Info("Processing Common Layer Files: Query");
        CodeGenUtils.CommonProjects.ProcessQuery(filesInModel, modelContainer, cCommonFolder);

        log.Info("Processing Common Layer Files: Exceptions");
        CodeGenUtils.CommonProjects.ProcessExceptions(filesInModel, modelContainer, cExceptionsFolder);

        log.Info("Processing Common Layer Files: Repository");
        CodeGenUtils.CommonProjects.ProcessRepository(filesInModel, modelContainer, cRepositoryFolder);

        log.Info("Processing Common Layer Files: Repository Container Interface");
        CodeGenUtils.CommonProjects.ProcessIRepositoryContainer(filesInModel, modelContainer, cIRepoContainerFolder);

        log.Info("Processing Common Layer Files: Repository Container");
        CodeGenUtils.CommonProjects.ProcessRepositoryContainer(filesInModel, modelContainer, cRepoContainerFolder);

        log.Info("Processing Models....");
        foreach(var m in modelContainer.Models)
            GenerateModel(modelContainer, m, host, tracker, log, filesInModel, cIRepoContainerFolder, cRepoContainerFolder);
    }

    private static void GenerateModel(ModelContainer modelContainer, Model m, IProjectHost host, ProjectTracker tracker, IProgressLog log, List<CodeFile> filesInModel, string cIRepoContainerFolder, string cRepoContainerFolder)
    {
        var commonSettings = modelContainer.CommonSettings;

        log.Info($"Processing Model {m.Name}: Adding To Common Layers");

        log.Info($"Processing Model {m.Name}: Adding to Repository Container Interface");
        CodeGenUtils.CommonProjects.ProcessModelIRepositoryContainer(filesInModel, modelContainer, m, cIRepoContainerFolder);

        log.Info($"Processing Model {m.Name}: Adding to Repository Container");
        CodeGenUtils.CommonProjects.ProcessModelRepositoryContainer(filesInModel, modelContainer, m, cRepoContainerFolder);

        log.Info($"Processing Model {m.Name}: Converting Schemas to Namespaces");
        foreach(var schema in ModelUtils.GetSchemasToProcess(m))
        {
            log.Info($"Processing Model {m.Name}: Adding Namespace {schema} to Repository Container Interface");
            CodeGenUtils.CommonProjects.ProcessSchemaIRepositoryContainer(filesInModel, modelContainer, m, schema, cIRepoContainerFolder);
            log.Info($"Processing Model {m.Name}: Adding Namespace {schema} to Repository Container");
            CodeGenUtils.CommonProjects.ProcessSchemaRepositoryContainer(filesInModel, modelContainer, m, schema, cRepoContainerFolder);
        }

        var entCat = m.Categories.GetByName(CategoryNames.Entities);
        var viewCat = m.Categories.GetByName(CategoryNames.Views);
        var procCat = m.Categories.GetByName(CategoryNames.Procs);
        var funcCat = m.Categories.GetByName(CategoryNames.Funcs);
        var seqCat = m.Categories.GetByName(CategoryNames.Seqs);

        var hasEntities = entCat.Count > 0;
        var hasViews = viewCat.Count > 0;
        var hasProcs = procCat.Count > 0;
        var hasProcEntities = m.HasStoredProcEntities;
        var hasFuncs = funcCat.Count > 0;
        var hasSeqs = seqCat.Count > 0;

        log.Info($"Processing Model {m.Name} Layers....");
        if(!hasEntities && !hasViews && !hasProcs && !hasFuncs && !hasSeqs)
        {
            log.Info($"Processing Model {m.Name}: No Database Artifacts Found");
            return;
        }

        var folders = new ModelFolders();

        log.Info($"Processing Model {m.Name} Project: Entities");
        folders.Entities = tracker.PrepareProject(m.Layers.Entity.FullProjectName(commonSettings), m.Name);

        if(hasEntities || hasViews || hasProcEntities)
        {//these are only processed if entities or views are found
            log.Info($"Processing Model {m.Name} Project: Entity Utils");
            folders.EntityUtils = tracker.PrepareProject(m.Layers.EntityUtil.FullProjectName(commonSettings), "Utils");
            folders.EntityUtils = IOUtils.CreateSubFolder(folders.EntityUtils, m.Name);

            log.Info($"Processing Model {m.Name} Project: Property Enumerations");
            folders.PropEnums = tracker.PrepareProject(m.Layers.PropEnum.FullProjectName(commonSettings), m.Name);

            log.Info($"Processing Model {m.Name} Project: Navigation Property Enumerations");
            folders.NavPropEnums = tracker.PrepareProject(m.Layers.NavPropEnum.FullProjectName(commonSettings), m.Name);
        }

        log.Info($"Processing Model {m.Name} Project: Repositories");
        folders.Repository = tracker.PrepareProject(m.Layers.Repository.FullProjectName(commonSettings), m.Name);

        log.Info($"Processing Model {m.Name} Project: Repository Interfaces");
        folders.IRepository = tracker.PrepareProject(m.Layers.IRepository.FullProjectName(commonSettings), m.Name);

        log.Info($"Processing Model {m.Name} References: Synchronizing Project References");
        host.PrepareReferences(modelContainer, m);

        log.Info($"Processing Model {m.Name} Database Artifacts....");
        if(hasEntities)
        {
            log.Info($"Processing Model {m.Name} Tables....");
            GenerateEntities(modelContainer, m, entCat, false, "Entities", folders, log, filesInModel);
        }

        if(hasViews)
        {
            log.Info($"Processing Model {m.Name} Views....");
            GenerateEntities(modelContainer, m, viewCat, true, "Views", folders, log, filesInModel);
        }

        if(hasProcs)
        {
            log.Info($"Processing Model {m.Name} Stored Procedures....");
            GenerateStoredProcs(modelContainer, m, procCat, folders, log, filesInModel);
        }

        if(hasFuncs)
        {
            log.Info($"Processing Model {m.Name} Database Functions....");
            GenerateFunctions(modelContainer, m, funcCat, folders, log, filesInModel);
        }

        if(hasSeqs)
        {
            log.Info($"Processing Model {m.Name} Database Sequences....");
            GenerateSequences(modelContainer, m, seqCat, folders, log, filesInModel);
        }
    }

    private static void GenerateEntities(ModelContainer modelContainer, Model m, Category category, bool isView, string label, ModelFolders folders, IProgressLog log, List<CodeFile> filesInModel)
    {
        var commonSettings = modelContainer.CommonSettings;

        foreach(var schema in category.Schemas)
        {
            if(schema.Count < 1)
            {
                log.Info($"Processing Model {m.Name} {label}: Namespace {schema.Name} Is Empty");
                continue;
            }

            //each project has schema subfolders to avoid naming conflicts
            log.Info($"Processing Model {m.Name} {label} Folders: For Namespace {schema.Name}");
            var entitiesSubFolder = IOUtils.CreateSubFolder(folders.Entities, schema.Name);

            log.Info($"Processing Model {m.Name} {label} Folders: EntityUtils For Namespace {schema.Name}");
            var entityUtilsSubFolder = IOUtils.CreateSubFolder(folders.EntityUtils, schema.Name);

            log.Info($"Processing Model {m.Name} {label} Folders: Property Enumerations For Namespace {schema.Name}");
            var propEnumsSubFolder = IOUtils.CreateSubFolder(folders.PropEnums, schema.Name);

            log.Info($"Processing Model {m.Name} {label} Folders: Navigation Property Enumerations For Namespace {schema.Name}");
            var navPropEnumsSubFolder = IOUtils.CreateSubFolder(folders.NavPropEnums, schema.Name);

            log.Info($"Processing Model {m.Name} {label} Folders: Repositories For Namespace {schema.Name}");
            var repositorySubFolder = IOUtils.CreateSubFolder(folders.Repository, schema.Name);

            log.Info($"Processing Model {m.Name} {label} Folders: Repository Interfaces For Namespace {schema.Name}");
            var iRepositorySubFolder = IOUtils.CreateSubFolder(folders.IRepository, schema.Name);

            log.Info($"Processing Model {m.Name} {label} Files: For Namespace {schema.Name}");
            var entities = isView ? schema.Views : schema.Entities;
            foreach(var ent in entities)
            {
                CodeGenUtils.ModelProjects.ProcessEntity(filesInModel, commonSettings, m, ent, schema, entitiesSubFolder);
                CodeGenUtils.ModelProjects.ProcessPropertyEnums(filesInModel, commonSettings, m, ent, schema, propEnumsSubFolder);
                CodeGenUtils.ModelProjects.ProcessNavPropertyEnums(filesInModel, commonSettings, m, ent, schema, navPropEnumsSubFolder);
                CodeGenUtils.ModelProjects.ProcessEntityUtils(filesInModel, modelContainer, m, ent, schema, entityUtilsSubFolder);
                CodeGenUtils.ModelProjects.ProcessIRepository(filesInModel, modelContainer, m, ent, schema, iRepositorySubFolder, isView);
                CodeGenUtils.ModelProjects.ProcessRepository(filesInModel, modelContainer, m, ent, schema, repositorySubFolder, isView);
            }
        }
    }

    private static void GenerateStoredProcs(ModelContainer modelContainer, Model m, Category category, ModelFolders folders, IProgressLog log, List<CodeFile> filesInModel)
    {
        var commonSettings = modelContainer.CommonSettings;

        foreach(var schema in category.Schemas)
        {
            if(schema.Count < 1)
            {
                log.Info($"Processing Model {m.Name} Stored Procedures: Namespace {schema.Name} Is Empty");
                continue;
            }

            log.Info($"Processing Model {m.Name} Stored Procedures Folders: For Namespace {schema.Name}");
            var entitiesSubFolder = IOUtils.CreateSubFolder(folders.Entities, schema.Name);

            log.Info($"Processing Model {m.Name} Stored Procedures Folders: Property Enumerations For Namespace {schema.Name}");
            var propEnumsSubFolder = IOUtils.CreateSubFolder(folders.PropEnums, schema.Name);

            log.Info($"Processing Model {m.Name} Stored Procedures Folders: Repositories For Namespace {schema.Name}");
            var repositorySubFolder = IOUtils.CreateSubFolder(folders.Repository, schema.Name);

            log.Info($"Processing Model {m.Name} Stored Procedures Folders: Repository Interfaces For Namespace {schema.Name}");
            var iRepositorySubFolder = IOUtils.CreateSubFolder(folders.IRepository, schema.Name);

            log.Info($"Processing Model {m.Name} Stored Procedures Projects: Repository Interfaces For Namespace {schema.Name}");
            CodeGenUtils.ModelProjects.ProcessIStoredProcRepository(filesInModel, modelContainer, m, schema, iRepositorySubFolder);

            log.Info($"Processing Model {m.Name} Stored Procedures Projects: Repositories For Namespace {schema.Name}");
            CodeGenUtils.ModelProjects.ProcessStoredProcRepository(filesInModel, modelContainer, m, schema, repositorySubFolder);

            log.Info($"Processing Model {m.Name} Stored Procedures Files: For Namespace {schema.Name}");
            foreach(var ent in schema.StoredProcs)
            {
                CodeGenUtils.ModelProjects.ProcessStoredProcEntity(filesInModel, commonSettings, m, ent, schema, entitiesSubFolder);
                CodeGenUtils.ModelProjects.ProcessStoredProcPropertyEnums(filesInModel, commonSettings, m, ent, schema, propEnumsSubFolder);
            }
        }
    }

    private static void GenerateFunctions(ModelContainer modelContainer, Model m, Category category, ModelFolders folders, IProgressLog log, List<CodeFile> filesInModel)
    {
        var commonSettings = modelContainer.CommonSettings;

        foreach(var schema in category.Schemas)
        {
            if(schema.Count < 1)
            {
                log.Info($"Processing Model {m.Name} Functions: Namespace {schema.Name} Is Empty");
                continue;
            }

            log.Info($"Processing Model {m.Name} Functions Folders: For Namespace {schema.Name}");
            var entitiesSubFolder = IOUtils.CreateSubFolder(folders.Entities, schema.Name);

            log.Info($"Processing Model {m.Name} Functions Folders: Repositories For Namespace {schema.Name}");
            var repositorySubFolder = IOUtils.CreateSubFolder(folders.Repository, schema.Name);

            log.Info($"Processing Model {m.Name} Functions Folders: Repository Interfaces For Namespace {schema.Name}");
            var iRepositorySubFolder = IOUtils.CreateSubFolder(folders.IRepository, schema.Name);

            log.Info($"Processing Model {m.Name} Functions Projects: Repository Interfaces For Namespace {schema.Name}");
            CodeGenUtils.ModelProjects.ProcessIFunctionRepository(filesInModel, modelContainer, m, schema, iRepositorySubFolder);

            log.Info($"Processing Model {m.Name} Functions Projects: Repositories For Namespace {schema.Name}");
            CodeGenUtils.ModelProjects.ProcessFunctionRepository(filesInModel, modelContainer, m, schema, repositorySubFolder);

            log.Info($"Processing Model {m.Name} Functions Files: For Namespace {schema.Name}");
            foreach(var ent in schema.Functions)
                CodeGenUtils.ModelProjects.ProcessFunctionEntity(filesInModel, commonSettings, m, ent, schema, entitiesSubFolder);
        }
    }

    private static void GenerateSequences(ModelContainer modelContainer, Model m, Category category, ModelFolders folders, IProgressLog log, List<CodeFile> filesInModel)
    {
        foreach(var schema in category.Schemas)
        {
            if(schema.Count < 1)
            {
                log.Info($"Processing Model {m.Name} Sequences: Namespace {schema.Name} Is Empty");
                continue;
            }

            log.Info($"Processing Model {m.Name} Sequences Folders: Repositories For Namespace {schema.Name}");
            var repositorySubFolder = IOUtils.CreateSubFolder(folders.Repository, schema.Name);

            log.Info($"Processing Model {m.Name} Sequences Folders: Repository Interfaces For Namespace {schema.Name}");
            var iRepositorySubFolder = IOUtils.CreateSubFolder(folders.IRepository, schema.Name);

            log.Info($"Processing Model {m.Name} Sequences Projects: Repository Interfaces For Namespace {schema.Name}");
            CodeGenUtils.ModelProjects.ProcessISequenceRepository(filesInModel, modelContainer, m, schema, iRepositorySubFolder);

            log.Info($"Processing Model {m.Name} Sequences Projects: Repositories For Namespace {schema.Name}");
            CodeGenUtils.ModelProjects.ProcessSequenceRepository(filesInModel, modelContainer, m, schema, repositorySubFolder);
        }
    }

    /// <summary>
    /// The folders of one model's projects.
    /// </summary>
    private sealed class ModelFolders
    {
        public string Entities { get; set; } = "";
        public string EntityUtils { get; set; } = "";
        public string PropEnums { get; set; } = "";
        public string NavPropEnums { get; set; } = "";
        public string Repository { get; set; } = "";
        public string IRepository { get; set; } = "";
    }

    /// <summary>
    /// Prepares projects through the host and remembers each one once.
    /// </summary>
    private sealed class ProjectTracker
    {
        private readonly IProjectHost _host;
        private readonly GenerationResult _result;

        public ProjectTracker(IProjectHost host, GenerationResult result)
        {
            _host = host;
            _result = result;
        }

        public string PrepareProject(string projectName, string subFolder)
        {
            var projectFolder = _host.PrepareProject(projectName);
            var key = projectName.ToUpper();
            if(!_result.Projects.ContainsKey(key))
            {
                _result.Projects.Add(key, projectName);
                _result.ProjectFolders.Add(key, projectFolder);
            }

            if(string.IsNullOrWhiteSpace(subFolder))
                return projectFolder;

            return IOUtils.CreateSubFolder(projectFolder, subFolder);
        }
    }
}

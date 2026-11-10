#region Usings
using SF.Models;
using SF.Utils;
using System.Collections.Generic;
#endregion

namespace SF.Engine;

/// <summary>
/// Builds and updates models from a database: what the app's Add and Update do.
/// </summary>
public static class ModelBuilder
{
    /// <summary>
    /// A new, empty model container with the given common settings and the default common layers.
    /// </summary>
    public static ModelContainer CreateContainer(string rootNamespace, string dependenciesFolder, int indentSize, bool useRootNamespaceInProjectName)
    {
        return new ModelContainer
        {
            CommonSettings = new CommonSettings
            {
                RootNamespace = ModelValidation.CleanName(rootNamespace),
                DependenciesFolder = ModelValidation.CleanName(dependenciesFolder),
                IndentSize = indentSize,
                UseRootNSInProjectName = useRootNamespaceInProjectName ? YesOrNo.Yes : YesOrNo.No,
            },
            CommonLayers = Defaults.CommonLayers(),
        };
    }

    /// <summary>
    /// Reads the database's tables, views, procedures, functions and sequences, for the user to choose from.
    /// </summary>
    public static DBObjectCategoryCollection ReadDatabaseObjects(ConnectionInfo conInfo)
    {
        return DbModelUtils.PopulateDbObjectModel(conInfo);
    }

    /// <summary>
    /// Reads the database objects of an existing model and selects the ones the model already has.
    /// </summary>
    public static DBObjectCategoryCollection ReadDatabaseObjectsForUpdate(Model model)
    {
        var dbCategories = DbModelUtils.PopulateDbObjectModel(model.ConInfo);
        DbModelUtils.OverlayCurrentModel(model, dbCategories);
        return dbCategories;
    }

    /// <summary>
    /// Builds a model from the selected database objects. The objects left out as not supported are written to the log.
    /// </summary>
    public static Model Populate(string modelName, ConnectionInfo conInfo, ModelLayers layers, DBObjectCategoryCollection dbCategories, IProgressLog log)
    {
        return Populate(modelName, conInfo, layers, dbCategories, log, null);
    }

    /// <summary>
    /// Builds a model from the selected database objects. The objects left out as not supported go to
    /// <paramref name="notSupported"/> when it is given (the caller reports them), otherwise to the log.
    /// <paramref name="dbCategories"/> is not changed, so building again from it reports each object once.
    /// </summary>
    public static Model Populate(string modelName, ConnectionInfo conInfo, ModelLayers layers, DBObjectCategoryCollection dbCategories, IProgressLog log, List<string> notSupported)
    {
        log.Info("Starting Model Generation Engine.....");

        //the reader's records (names with characters that are not supported) and the builder's (names that cannot
        //become C# names)
        var records = new List<string>(dbCategories.NotSupported);

        var model = new Model
        {
            Name = modelName,
            ConInfo = conInfo,
            Categories = Defaults.Categories(),
            Layers = layers,
        };

        var tableCategory = dbCategories.GetByName(DbCategoryNames.Entities);
        var viewCategory = dbCategories.GetByName(DbCategoryNames.Views);
        var procCategory = dbCategories.GetByName(DbCategoryNames.Procs);
        var funcCategory = dbCategories.GetByName(DbCategoryNames.Funcs);
        var seqCategory = dbCategories.GetByName(DbCategoryNames.Seqs);

        log.Info("Preprocess Tables From Database.....");
        var selectedTableSchemas = DbModelUtils.GetSelectedDbObjects(tableCategory);
        if(selectedTableSchemas?.Count > 0)
        {
            log.Info("Generating Entities.....");
            log.Info("Generating Properties.....");
            log.Info("Generating Primary Keys and Unique Keys.....");
            ModelUtils.PopulateEntitiesPropsAndKeys(model, selectedTableSchemas, ModelObjectType.Entities, records);

            log.Info("Generating Navigation Properties.....");
            ModelUtils.PopulateForeignKeysAndNavProps(model, selectedTableSchemas);

            log.Info("Generating Identity Properties.....");
            log.Info("Generating Computed Properties.....");
            ModelUtils.ApplyIdentityComputedValues(model, selectedTableSchemas);
        }

        log.Info("Preprocess Views From Database.....");
        var selectedViewSchemas = DbModelUtils.GetSelectedDbObjects(viewCategory);
        if(selectedViewSchemas?.Count > 0)
        {
            log.Info("Generating Views.....");
            log.Info("Generating Properties.....");
            ModelUtils.PopulateEntitiesPropsAndKeys(model, selectedViewSchemas, ModelObjectType.Views, records);
        }

        log.Info("Preprocess Stored Procedures From Database.....");
        var selectedProcSchemas = DbModelUtils.GetSelectedDbObjects(procCategory);
        if(selectedProcSchemas?.Count > 0)
        {
            log.Info("Generating Stored Procedures.....");
            log.Info("Generating Stored Procedure Parameters.....");
            log.Info("Generating Stored Procedure Return Entities.....");

            if(!ModelUtils.PopulateStoredProcsAndParams(model, selectedProcSchemas, records))
                log.Error("Unable to process one or more Stored Procedures.", "Unable to process one or more SP(s). Check Error Log.");
        }

        log.Info("Preprocess Functions From Database.....");
        var selectedFuncSchemas = DbModelUtils.GetSelectedDbObjects(funcCategory);
        if(selectedFuncSchemas?.Count > 0)
        {
            log.Info("Generating Functions.....");
            log.Info("Generating Function Parameters.....");
            log.Info("Generating Function Return Entities.....");

            if(!ModelUtils.PopulateFunctionsAndParams(model, selectedFuncSchemas, records))
                log.Error("Unable to process one or more Functions.", "Unable to process one or more Function(s). Check Error Log.");
        }

        log.Info("Preprocess Sequences From Database.....");
        var selectedSeqSchemas = DbModelUtils.GetSelectedDbObjects(seqCategory);
        if(selectedSeqSchemas?.Count > 0)
        {
            log.Info("Generating Sequences.....");

            if(!ModelUtils.PopulateSequences(model, selectedSeqSchemas, records))
                log.Error("Unable to process one or more Sequences.", "Unable to process one or more Sequence(s). Check Error Log.");
        }

        if(notSupported != null)
        {
            notSupported.AddRange(records);
        }
        else
        {
            foreach(var record in records)
                log.Info("Not supported, left out: " + record);
        }

        return model;
    }

    /// <summary>
    /// Rebuilds an existing model from the selected database objects, keeping its connection and layers.
    /// </summary>
    public static Model Update(Model currentModel, DBObjectCategoryCollection dbCategories, IProgressLog log)
    {
        return Update(currentModel, dbCategories, log, null);
    }

    /// <summary>
    /// Rebuilds an existing model from the selected database objects, keeping its connection and layers; the objects
    /// left out as not supported go to <paramref name="notSupported"/> when it is given, otherwise to the log.
    /// </summary>
    public static Model Update(Model currentModel, DBObjectCategoryCollection dbCategories, IProgressLog log, List<string> notSupported)
    {
        return Populate(currentModel.Name, currentModel.ConInfo.Clone(), currentModel.Layers.Clone(), dbCategories, log, notSupported);
    }

    /// <summary>
    /// Replaces a model in its container with its updated version, in the same place (the file keeps its order).
    /// </summary>
    public static void ReplaceModel(ModelContainer modelContainer, Model currentModel, Model updatedModel)
    {
        var index = modelContainer.Models.IndexOf(currentModel);
        if(index < 0)
        {
            modelContainer.Models.Add(updatedModel);
            return;
        }

        modelContainer.Models.RemoveAt(index);
        modelContainer.Models.Insert(index, updatedModel);
    }
}

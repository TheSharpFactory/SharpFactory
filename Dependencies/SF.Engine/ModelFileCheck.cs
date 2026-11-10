#region Usings
using SF.Common.Logging;
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
#endregion

namespace SF.Engine;

/// <summary>
/// Opens a model file and makes sure it can be used, so a broken or hand-edited file gives a clear reason instead of
/// failing later in the middle of generating or updating. A model that only lacks a whole category (no sequences, say)
/// gets that category back, empty: that is what an empty category means anyway.
/// </summary>
public static class ModelFileCheck
{
    /// <summary>
    /// The model file, or null with the reason it cannot be used.
    /// </summary>
    public static ModelContainer Load(string edmjFile, out string problem)
    {
        var container = (ModelContainer)null;
        try
        {
            container = ModelUtils.Load(edmjFile);
        }
        catch(Exception ex)
        {
            LogManager.WriteErrorLog(ex, "Unable to read the model file", edmjFile);
            problem = $"It is not a model file: {FirstLine(ex.Message)}";
            return null;
        }

        if(container == null)
        {
            problem = "The file is empty or is not a model file.";
            return null;
        }

        problem = Check(container);
        return problem == null ? container : null;
    }

    /// <summary>
    /// Null when the container can be used (after adding back missing empty categories); otherwise what is missing.
    /// </summary>
    public static string Check(ModelContainer container)
    {
        if(container.CommonSettings == null)
            return "It has no common settings.";

        var missing = MissingLayer(container.CommonLayers);
        if(missing != null)
            return $"Its common layers are incomplete: {missing}.";

        if(container.Models == null)
            return "It has no list of models.";

        foreach(var model in container.Models)
        {
            if(model == null || string.IsNullOrWhiteSpace(model.Name))
                return "A model has no name.";

            if(model.ConInfo == null || string.IsNullOrWhiteSpace(model.ConInfo.Server) || string.IsNullOrWhiteSpace(model.ConInfo.Database))
                return $"Model {model.Name} has no server and database.";

            missing = MissingLayer(model.Layers);
            if(missing != null)
                return $"The layers of model {model.Name} are incomplete: {missing}.";

            model.Categories ??= new CategoryCollection();
            foreach(var category in Defaults.Categories())
            {
                if(!model.Categories.Contains(category.Name))
                    model.Categories.Add(category);
            }
        }

        return null;
    }

    /// <summary>
    /// The names of the models marked as synced. Sync generates every model of the file, so one such model turns it
    /// off for the file.
    /// </summary>
    public static List<string> SyncedModelNames(ModelContainer container)
    {
        var result = new List<string>();
        if(container == null || container.Models == null)
            return result;

        foreach(var model in container.Models)
        {
            if(model.Synced == YesOrNo.Yes)
                result.Add(model.Name);
        }

        return result;
    }

    /// <summary>
    /// Why Sync is off for the file: the models marked as synced. Null when no model is marked.
    /// </summary>
    public static string SyncBlockText(ModelContainer container)
    {
        var names = SyncedModelNames(container);
        if(names.Count == 0)
            return null;

        var which = names.Count == 1 ? $"Model {names[0]} is" : $"Models {string.Join(", ", names)} are";
        return $"{which} marked as synced, so Sync is turned off for this file.";
    }

    /// <summary>
    /// The first layer of <paramref name="layers"/> (common or model layers) that is not there; "all of them" when the
    /// layers themselves are missing; null when every layer is there.
    /// </summary>
    private static string MissingLayer(object layers)
    {
        if(layers == null)
            return "all of them";

        foreach(var property in layers.GetType().GetProperties())
        {
            if(property.PropertyType == typeof(LayerInfo) && property.GetValue(layers) == null)
                return property.Name;
        }

        return null;
    }

    private static string FirstLine(string message)
    {
        var text = message ?? "";
        var end = text.IndexOfAny(new[] { '\r', '\n' });
        return end < 0 ? text : text.Substring(0, end);
    }
}

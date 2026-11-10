#region Usings
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
#endregion

namespace SF.Engine;

/// <summary>
/// Reads a model's objects again from its database, as the app's Update does with every object kept: the command
/// line's update. Objects the database no longer has drop out of the model; new objects are not added.
/// </summary>
public static class ModelUpdater
{
    private const string _procedures = "Procedures: ";

    public static ModelUpdateResult Update(ModelContainer modelContainer, Model model, IProgressLog log)
    {
        var before = ObjectNames(model);
        var beforeJson = ModelUtils.ToJson(model);

        var dbCategories = ModelBuilder.ReadDatabaseObjectsForUpdate(model);
        var notSupported = new List<string>();
        var updated = ModelBuilder.Update(model, dbCategories, log, notSupported);
        ModelBuilder.ReplaceModel(modelContainer, model, updated);

        var after = new HashSet<string>(ObjectNames(updated), StringComparer.OrdinalIgnoreCase);
        var result = new ModelUpdateResult { ModelName = updated.Name, Model = updated };
        foreach(var name in before)
        {
            var record = FindNotSupportedRecord(name, notSupported);
            if(record != null)
                result.NotSupported.Add(record);
            else if(!after.Contains(name))
                result.Removed.Add(name);
        }

        result.IsChanged = !string.Equals(Canonical(beforeJson, model), Canonical(ModelUtils.ToJson(updated), updated), StringComparison.Ordinal);
        return result;
    }

    /// <summary>
    /// The model's JSON with each foreign key's id replaced by its place in the model: the ids are numbered afresh on
    /// every read (they belong to the model, not the database), so they alone are not a change.
    /// </summary>
    private static string Canonical(string json, Model model)
    {
        var places = new Dictionary<int, int>();
        if(model.FKeys != null)
        {
            for(var i = 0; i < model.FKeys.Count; i++)
                places[model.FKeys[i].Id] = i;
        }

        var evaluator = new MatchEvaluator(new FKeyIdRenumberer(places).Replace);
        return Regex.Replace(json, "\"(FKId|Id)\"(\\s*):(\\s*)(-?\\d+)", evaluator);
    }

    private sealed class FKeyIdRenumberer
    {
        private readonly Dictionary<int, int> _places;

        public FKeyIdRenumberer(Dictionary<int, int> places)
        {
            _places = places;
        }

        public string Replace(Match match)
        {
            var id = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            if(!_places.TryGetValue(id, out var place))
                return match.Value;

            return $"\"{match.Groups[1].Value}\"{match.Groups[2].Value}:{match.Groups[3].Value}#{place}";
        }
    }

    /// <summary>
    /// The record of <paramref name="notSupported"/> about the model object <paramref name="name"/> (same category,
    /// schema and name), or null. Only objects the model had are reported: the database's other unsupported objects
    /// were never in it.
    /// </summary>
    private static string FindNotSupportedRecord(string name, List<string> notSupported)
    {
        //the model's names say "Procedures", the records "Stored procedures"
        var recordName = name.StartsWith(_procedures, StringComparison.Ordinal) ? "Stored procedures: " + name.Substring(_procedures.Length) : name;
        foreach(var record in notSupported)
        {
            if(record.StartsWith(recordName + " (", StringComparison.Ordinal))
                return record;
        }

        return null;
    }

    /// <summary>
    /// "Tables: Sales.Customer" for every object of the model, in the model's order.
    /// </summary>
    public static List<string> ObjectNames(Model model)
    {
        var result = new List<string>();
        AddNames(result, model, CategoryNames.Entities, "Tables");
        AddNames(result, model, CategoryNames.Views, "Views");
        AddNames(result, model, CategoryNames.Procs, "Procedures");
        AddNames(result, model, CategoryNames.Funcs, "Functions");
        AddNames(result, model, CategoryNames.Seqs, "Sequences");
        return result;
    }

    private static void AddNames(List<string> result, Model model, string categoryName, string label)
    {
        var category = model.Categories?.GetByName(categoryName);
        if(category?.Schemas == null)
            return;

        foreach(var schema in category.Schemas)
        {
            foreach(var name in SchemaObjectNames(schema, categoryName))
                result.Add($"{label}: {schema.Name}.{name}");
        }
    }

    private static List<string> SchemaObjectNames(Schema schema, string categoryName)
    {
        var names = new List<string>();
        if(string.Equals(categoryName, CategoryNames.Entities, StringComparison.Ordinal))
        {
            foreach(var entity in schema.Entities)
                names.Add(entity.SN);
        }
        else if(string.Equals(categoryName, CategoryNames.Views, StringComparison.Ordinal))
        {
            foreach(var view in schema.Views)
                names.Add(view.SN);
        }
        else if(string.Equals(categoryName, CategoryNames.Procs, StringComparison.Ordinal))
        {
            foreach(var proc in schema.StoredProcs)
                names.Add(proc.SN);
        }
        else if(string.Equals(categoryName, CategoryNames.Funcs, StringComparison.Ordinal))
        {
            foreach(var func in schema.Functions)
                names.Add(func.SN);
        }
        else
        {
            foreach(var seq in schema.Sequences)
                names.Add(seq.SN);
        }

        return names;
    }
}

/// <summary>
/// What an update did to one model.
/// </summary>
public sealed class ModelUpdateResult
{
    public string ModelName { get; set; }
    public Model Model { get; set; }

    /// <summary>
    /// True when the model is different after the update (an object or a column changed, or an object dropped out).
    /// </summary>
    public bool IsChanged { get; set; }

    /// <summary>
    /// The objects the database no longer has: "Tables: Sales.OldOrders".
    /// </summary>
    public List<string> Removed { get; } = new List<string>();

    /// <summary>
    /// The model's objects left out because their names are not supported:
    /// "Tables: dbo.Odd (column '_' cannot become a C# name)".
    /// </summary>
    public List<string> NotSupported { get; } = new List<string>();
}

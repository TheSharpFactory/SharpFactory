#region Usings
using SF.Engine;
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace SF.SchemaCheck;

/// <summary>
/// Regression check for the schema reader.
/// Usage: SF.SchemaCheck &lt;model.edmj&gt; [model name ...]
/// For each model (all, or the ones named) it re-reads the selected tables, views, stored procedures, functions and
/// sequences from the model's database, the same way the app's Update does (PopulateDbObjectModel, OverlayCurrentModel,
/// then the ModelUtils readers), and compares them with the model stored in the .edmj.
/// Read-only on the database. Exit code 0 only when every model matches; 1 otherwise; 2 for usage errors.
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        if(args.Length < 1)
        {
            Console.WriteLine("Usage: SF.SchemaCheck <model.edmj> [model name ...]");
            return 2;
        }

        var modelContainer = ModelUtils.Load(args[0]);
        if(modelContainer == null)
        {
            Console.WriteLine($"Unable to load the model: {args[0]}");
            return 2;
        }

        var names = args.Skip(1).ToList();
        var models = new List<Model>();
        foreach(var m in modelContainer.Models)
        {
            if(names.Count == 0 || names.Contains(m.Name, StringComparer.OrdinalIgnoreCase))
                models.Add(m);
        }

        if(models.Count == 0)
        {
            Console.WriteLine("No matching model in the .edmj.");
            return 2;
        }

        var allMatch = true;
        foreach(var model in models)
        {
            if(!CheckModel(model))
                allMatch = false;
        }

        return allMatch ? 0 : 1;
    }

    private static bool CheckModel(Model stored)
    {
        var conInfo = stored.ConInfo;
        Console.WriteLine($"Model {stored.Name}: {conInfo.Server} / {conInfo.Database}");

        if(!DbModelUtils.ValidateConnectionInfo(conInfo, out var reason))
        {
            Console.WriteLine($"   cannot connect to the database: {reason}");
            return false;
        }

        var fresh = ReadFromDatabase(stored);
        var differences = new List<string>();

        CompareEntities(differences, "table", AllEntities(stored, CategoryNames.Entities), AllEntities(fresh, CategoryNames.Entities));
        CompareEntities(differences, "view", AllEntities(stored, CategoryNames.Views), AllEntities(fresh, CategoryNames.Views));
        CompareLists(differences, "foreign keys", DescribeFKeys(stored), DescribeFKeys(fresh));
        CompareStoredProcs(differences, stored, fresh);
        CompareFunctions(differences, stored, fresh);
        CompareSequences(differences, stored, fresh);

        Console.WriteLine($"   {CountObjects(stored, CategoryNames.Entities)} tables, {CountObjects(stored, CategoryNames.Views)} views, " +
            $"{CountObjects(stored, CategoryNames.Procs)} procedures, {CountObjects(stored, CategoryNames.Funcs)} functions, " +
            $"{CountObjects(stored, CategoryNames.Seqs)} sequences, {stored.FKeys.Count} foreign keys in the model; {differences.Count} differences");

        foreach(var difference in differences)
            Console.WriteLine("   " + difference);

        return differences.Count == 0;
    }

    private static Model ReadFromDatabase(Model stored)
    {
        var conInfo = stored.ConInfo;
        var dbCategories = DbModelUtils.PopulateDbObjectModel(conInfo);
        DbModelUtils.OverlayCurrentModel(stored, dbCategories);

        var fresh = new Model { Name = stored.Name, ConInfo = conInfo, Categories = Defaults.Categories(), Layers = stored.Layers };

        var tables = DbModelUtils.GetSelectedDbObjects(dbCategories.GetByName(DbCategoryNames.Entities));
        if(tables?.Count > 0)
        {
            ModelUtils.PopulateEntitiesPropsAndKeys(fresh, tables, ModelObjectType.Entities);
            ModelUtils.PopulateForeignKeysAndNavProps(fresh, tables);
            ModelUtils.ApplyIdentityComputedValues(fresh, tables);
        }

        var views = DbModelUtils.GetSelectedDbObjects(dbCategories.GetByName(DbCategoryNames.Views));
        if(views?.Count > 0)
            ModelUtils.PopulateEntitiesPropsAndKeys(fresh, views, ModelObjectType.Views);

        var procs = DbModelUtils.GetSelectedDbObjects(dbCategories.GetByName(DbCategoryNames.Procs));
        if(procs?.Count > 0)
            ModelUtils.PopulateStoredProcsAndParams(fresh, procs);

        var funcs = DbModelUtils.GetSelectedDbObjects(dbCategories.GetByName(DbCategoryNames.Funcs));
        if(funcs?.Count > 0)
            ModelUtils.PopulateFunctionsAndParams(fresh, funcs);

        var seqs = DbModelUtils.GetSelectedDbObjects(dbCategories.GetByName(DbCategoryNames.Seqs));
        if(seqs?.Count > 0)
            ModelUtils.PopulateSequences(fresh, seqs);

        return fresh;
    }

    private static void CompareEntities(List<string> differences, string kind, Dictionary<string, Entity> stored, Dictionary<string, Entity> fresh)
    {
        CompareLists(differences, kind + "s", SortedKeys(stored.Keys), SortedKeys(fresh.Keys));

        foreach(var name in SortedCommonKeys(stored.Keys, fresh.Keys))
        {
            var s = stored[name];
            var f = fresh[name];

            CompareLists(differences, $"{kind} {name} columns", DescribeProperties(s.Ps), DescribeProperties(f.Ps));
            CompareLists(differences, $"{kind} {name} primary key", new[] { Describe(s.PK) }, new[] { Describe(f.PK) });
            CompareLists(differences, $"{kind} {name} unique keys", DescribeKeys(s.UKs), DescribeKeys(f.UKs));
            CompareLists(differences, $"{kind} {name} navigation properties", DescribeNavProps(s.NPs), DescribeNavProps(f.NPs));
            CompareLists(differences, $"{kind} {name} flags", new[] { $"identity={s.HId} triggers={s.HTr}" }, new[] { $"identity={f.HId} triggers={f.HTr}" });
        }
    }

    private static void CompareStoredProcs(List<string> differences, Model stored, Model fresh)
    {
        var s = AllStoredProcs(stored);
        var f = AllStoredProcs(fresh);

        CompareLists(differences, "stored procedures", SortedKeys(s.Keys), SortedKeys(f.Keys));

        foreach(var name in SortedCommonKeys(s.Keys, f.Keys))
        {
            CompareLists(differences, $"procedure {name} parameters", DescribeProperties(s[name].Par), DescribeProperties(f[name].Par));
            CompareLists(differences, $"procedure {name} result columns", DescribeProperties(s[name].En?.Ps), DescribeProperties(f[name].En?.Ps));
        }
    }

    private static void CompareFunctions(List<string> differences, Model stored, Model fresh)
    {
        var s = AllFunctions(stored);
        var f = AllFunctions(fresh);

        CompareLists(differences, "functions", SortedKeys(s.Keys), SortedKeys(f.Keys));

        foreach(var name in SortedCommonKeys(s.Keys, f.Keys))
        {
            CompareLists(differences, $"function {name} parameters", DescribeProperties(s[name].Par), DescribeProperties(f[name].Par));
            CompareLists(differences, $"function {name} table columns", DescribeProperties(s[name].En?.Ps), DescribeProperties(f[name].En?.Ps));
            CompareLists(differences, $"function {name} scalar return", new[] { Describe(s[name].Sc) }, new[] { Describe(f[name].Sc) });
        }
    }

    private static void CompareSequences(List<string> differences, Model stored, Model fresh)
    {
        var s = AllSequences(stored);
        var f = AllSequences(fresh);

        CompareLists(differences, "sequences", SortedKeys(s.Keys), SortedKeys(f.Keys));

        foreach(var name in SortedCommonKeys(s.Keys, f.Keys))
            CompareLists(differences, $"sequence {name} type", new[] { Describe(s[name].Sc) }, new[] { Describe(f[name].Sc) });
    }

    private static void CompareLists(List<string> differences, string what, IEnumerable<string> stored, IEnumerable<string> fresh)
    {
        //repeated items count: a column listed twice on one side is a difference, not "a different order"
        differences.AddRange(ListComparison.Describe(what, stored, fresh));
    }

    #region Collecting
    private static Dictionary<string, Entity> AllEntities(Model model, string category)
    {
        var result = new Dictionary<string, Entity>();
        foreach(var schema in model.Categories.GetByName(category).Schemas)
        {
            var entities = string.Equals(category, CategoryNames.Views, StringComparison.Ordinal) ? schema.Views : schema.Entities;
            if(entities == null)
                continue;

            foreach(var e in entities)
                result.Add(schema.Name + "." + e.SN, e);
        }

        return result;
    }

    private static Dictionary<string, StoredProc> AllStoredProcs(Model model)
    {
        var result = new Dictionary<string, StoredProc>();
        foreach(var schema in model.Categories.GetByName(CategoryNames.Procs).Schemas)
        {
            if(schema.StoredProcs == null)
                continue;

            foreach(var p in schema.StoredProcs)
                result.Add(schema.Name + "." + p.SN, p);
        }

        return result;
    }

    private static Dictionary<string, Function> AllFunctions(Model model)
    {
        var result = new Dictionary<string, Function>();
        foreach(var schema in model.Categories.GetByName(CategoryNames.Funcs).Schemas)
        {
            if(schema.Functions == null)
                continue;

            foreach(var f in schema.Functions)
                result.Add(schema.Name + "." + f.SN, f);
        }

        return result;
    }

    private static Dictionary<string, Sequence> AllSequences(Model model)
    {
        var result = new Dictionary<string, Sequence>();
        foreach(var schema in model.Categories.GetByName(CategoryNames.Seqs).Schemas)
        {
            if(schema.Sequences == null)
                continue;

            foreach(var q in schema.Sequences)
                result.Add(schema.Name + "." + q.SN, q);
        }

        return result;
    }

    private static int CountObjects(Model model, string category)
    {
        var count = 0;
        foreach(var schema in model.Categories.GetByName(category).Schemas)
        {
            switch(category)
            {
                case CategoryNames.Entities:
                    count += schema.Entities?.Count ?? 0;
                    break;
                case CategoryNames.Views:
                    count += schema.Views?.Count ?? 0;
                    break;
                case CategoryNames.Procs:
                    count += schema.StoredProcs?.Count ?? 0;
                    break;
                case CategoryNames.Funcs:
                    count += schema.Functions?.Count ?? 0;
                    break;
                case CategoryNames.Seqs:
                    count += schema.Sequences?.Count ?? 0;
                    break;
            }
        }

        return count;
    }

    private static List<string> SortedKeys(IEnumerable<string> keys)
    {
        var result = new List<string>(keys);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static List<string> SortedCommonKeys(IEnumerable<string> first, IEnumerable<string> second)
    {
        var result = first.Intersect(second).ToList();
        result.Sort(StringComparer.Ordinal);
        return result;
    }
    #endregion

    #region Describing
    private static List<string> DescribeProperties(IEnumerable<Property> properties)
    {
        if(properties == null)
            return null;

        var result = new List<string>();
        foreach(var p in properties)
            result.Add(Describe(p));

        return result;
    }

    private static List<string> DescribeKeys(IEnumerable<Key> keys)
    {
        if(keys == null)
            return null;

        var result = new List<string>();
        foreach(var k in keys)
            result.Add(Describe(k));

        return result;
    }

    private static List<string> DescribeNavProps(IEnumerable<NavProperty> navProps)
    {
        if(navProps == null)
            return null;

        var result = new List<string>();
        foreach(var np in navProps)
            result.Add(Describe(np));

        return result;
    }

    private static List<string> DescribeFKeys(Model model)
    {
        var result = new List<string>();
        foreach(var f in model.FKeys)
            result.Add(Describe(f));

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static string Describe(Property p)
    {
        if(p == null)
            return "-";

        return $"{p.SN}:{p.ST}{(p.S.HasValue ? "(" + p.S + ")" : "")}{(p.Nul == YesOrNo.Yes ? " null" : "")}{(p.Ide == YesOrNo.Yes ? " identity" : "")}" +
            $"{(p.IsComp == YesOrNo.Yes ? " computed" : "")}{(p.IsGen == YesOrNo.Yes ? " generated" : "")}{(p.M.HasValue ? " " + p.M : "")}{(p.Def != null ? " default=" + p.Def : "")}";
    }

    private static string Describe(Key k)
    {
        if(k == null)
            return "-";

        var names = new List<string>();
        foreach(var p in k.Ps)
            names.Add(p.N);

        return $"{k.N}({string.Join(",", names)})";
    }

    private static string Describe(NavProperty np)
    {
        return $"{np.N}[{np.Mul}/{np.End}]";
    }

    private static string Describe(FKey f)
    {
        var parts = new List<string>();
        foreach(var p in f.FKParts)
            parts.Add(p.CCol + "=" + p.PCol);

        return $"{f.N}: {f.CSc}.{f.CT} -> {f.PSc}.{f.PT} [{string.Join(",", parts)}] child {f.CMu}";
    }
    #endregion
}

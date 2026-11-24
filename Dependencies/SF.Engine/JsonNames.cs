#region Usings
using SF.Models;
using System;
using System.Collections.Generic;
#endregion

namespace SF.Engine;

/// <summary>
/// The short JSON names of Optimize JSON (A..Z, A0..A9, AA..), kept in the model so a name never changes: a property
/// keeps the name it was given (<see cref="Property.JN"/>, <see cref="NavProperty.JN"/>), and a new one gets the next
/// name after the last its class gave (the class's JL), so the name of a removed column is not given again.
/// </summary>
public static class JsonNames
{
    /// <summary>
    /// Gives a name to every property of the Entities project's classes that has none, when Optimize JSON is on: the
    /// columns in order, then the navigation properties. Returns how many names it gave (0 when the setting is off or
    /// every property has one), so the caller knows the model changed.
    /// </summary>
    public static int Assign(ModelContainer modelContainer)
    {
        if(modelContainer?.CommonSettings == null || modelContainer.CommonSettings.OptimizeJson != YesOrNo.Yes || modelContainer.Models == null)
            return 0;

        var given = 0;
        foreach(var model in modelContainer.Models)
        {
            if(model?.Categories == null)
                continue;

            given += AssignEntities(model, CategoryNames.Entities, false);
            given += AssignEntities(model, CategoryNames.Views, true);
            given += AssignStoredProcs(model);
            given += AssignFunctions(model);
        }

        return given;
    }

    /// <summary>
    /// Copies the names of <paramref name="current"/> to <paramref name="updated"/>, the same model rebuilt from the
    /// database: the same class and property (by name) keep their name and their class's last name.
    /// </summary>
    public static void CarryOver(Model current, Model updated)
    {
        if(current?.Categories == null || updated?.Categories == null)
            return;

        CarryOverEntities(current, updated, CategoryNames.Entities, false);
        CarryOverEntities(current, updated, CategoryNames.Views, true);
        CarryOverStoredProcs(current, updated);
        CarryOverFunctions(current, updated);
    }

    #region Assign
    private static int AssignEntities(Model model, string categoryName, bool isView)
    {
        var category = FindCategory(model, categoryName);
        if(category?.Schemas == null)
            return 0;

        var given = 0;
        foreach(var schema in category.Schemas)
        {
            var entities = isView ? schema.Views : schema.Entities;
            if(entities == null)
                continue;

            foreach(var entity in entities)
                given += AssignEntity(entity);
        }

        return given;
    }

    private static int AssignStoredProcs(Model model)
    {
        var category = FindCategory(model, CategoryNames.Procs);
        if(category?.Schemas == null)
            return 0;

        var given = 0;
        foreach(var schema in category.Schemas)
        {
            if(schema.StoredProcs == null)
                continue;

            foreach(var proc in schema.StoredProcs)
            {
                if(proc.HPar)
                {
                    var classNames = new ClassNames(proc.JL);
                    given += classNames.Give(proc.Par, null);
                    proc.JL = classNames.Last;
                }

                if(proc.En != null)
                    given += AssignEntity(proc.En);
            }
        }

        return given;
    }

    private static int AssignFunctions(Model model)
    {
        var category = FindCategory(model, CategoryNames.Funcs);
        if(category?.Schemas == null)
            return 0;

        var given = 0;
        foreach(var schema in category.Schemas)
        {
            if(schema.Functions == null)
                continue;

            foreach(var func in schema.Functions)
            {
                if(func.Par?.Count > 0)
                {
                    var classNames = new ClassNames(func.JL);
                    given += classNames.Give(func.Par, null);
                    func.JL = classNames.Last;
                }

                if(func.En != null)
                    given += AssignEntity(func.En);
            }
        }

        return given;
    }

    private static int AssignEntity(Entity entity)
    {
        var classNames = new ClassNames(entity.JL);
        var given = classNames.Give(entity.Ps, entity.NPs);
        entity.JL = classNames.Last;
        return given;
    }

    private static Category FindCategory(Model model, string categoryName)
    {
        if(!model.Categories.Contains(categoryName))
            return null;

        return model.Categories.GetByName(categoryName);
    }

    /// <summary>
    /// The names of one class: each property without a name gets the next one after the class's last name, skipping
    /// the names its other properties have.
    /// </summary>
    private sealed class ClassNames
    {
        private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);
        private IdSequence _sequence = null;

        public ClassNames(string last)
        {
            Last = last;
        }

        /// <summary>
        /// The last name the class gave, the class's JL.
        /// </summary>
        public string Last { get; private set; }

        public int Give(PropertyCollection props, NavPropertyCollection navProps)
        {
            AddUsed(props, navProps);

            var given = 0;
            if(props != null)
            {
                foreach(var p in props)
                {
                    if(p.JN != null)
                        continue;

                    p.JN = TakeNext();
                    given++;
                }
            }

            if(navProps != null)
            {
                foreach(var np in navProps)
                {
                    if(np.JN != null)
                        continue;

                    np.JN = TakeNext();
                    given++;
                }
            }

            return given;
        }

        private void AddUsed(PropertyCollection props, NavPropertyCollection navProps)
        {
            if(props != null)
            {
                foreach(var p in props)
                {
                    if(p.JN != null)
                        _used.Add(p.JN);
                }
            }

            if(navProps != null)
            {
                foreach(var np in navProps)
                {
                    if(np.JN != null)
                        _used.Add(np.JN);
                }
            }
        }

        private string TakeNext()
        {
            if(_sequence == null)
            {
                _sequence = new IdSequence();
                if(!string.IsNullOrEmpty(Last))
                    _sequence.Seed(Last);
            }

            var name = _sequence.NextId();
            while(_used.Contains(name))
                name = _sequence.NextId();

            _used.Add(name);
            Last = name;
            return name;
        }
    }
    #endregion

    #region Carry over
    private static void CarryOverEntities(Model current, Model updated, string categoryName, bool isView)
    {
        var currentCategory = FindCategory(current, categoryName);
        var updatedCategory = FindCategory(updated, categoryName);
        if(currentCategory?.Schemas == null || updatedCategory?.Schemas == null)
            return;

        foreach(var schema in updatedCategory.Schemas)
        {
            if(!currentCategory.Schemas.Contains(schema.Name))
                continue;

            var currentSchema = currentCategory.Schemas.GetByName(schema.Name);
            var entities = isView ? schema.Views : schema.Entities;
            var currentEntities = isView ? currentSchema.Views : currentSchema.Entities;
            if(entities == null || currentEntities == null)
                continue;

            foreach(var entity in entities)
            {
                if(currentEntities.Contains(entity.N))
                    CarryOverEntity(currentEntities.GetByName(entity.N), entity);
            }
        }
    }

    private static void CarryOverStoredProcs(Model current, Model updated)
    {
        var currentCategory = FindCategory(current, CategoryNames.Procs);
        var updatedCategory = FindCategory(updated, CategoryNames.Procs);
        if(currentCategory?.Schemas == null || updatedCategory?.Schemas == null)
            return;

        foreach(var schema in updatedCategory.Schemas)
        {
            if(!currentCategory.Schemas.Contains(schema.Name))
                continue;

            var currentProcs = currentCategory.Schemas.GetByName(schema.Name).StoredProcs;
            if(schema.StoredProcs == null || currentProcs == null)
                continue;

            foreach(var proc in schema.StoredProcs)
            {
                if(!currentProcs.Contains(proc.N))
                    continue;

                var currentProc = currentProcs.GetByName(proc.N);
                proc.JL = currentProc.JL;
                CarryOverProps(currentProc.Par, proc.Par);
                if(currentProc.En != null && proc.En != null)
                    CarryOverEntity(currentProc.En, proc.En);
            }
        }
    }

    private static void CarryOverFunctions(Model current, Model updated)
    {
        var currentCategory = FindCategory(current, CategoryNames.Funcs);
        var updatedCategory = FindCategory(updated, CategoryNames.Funcs);
        if(currentCategory?.Schemas == null || updatedCategory?.Schemas == null)
            return;

        foreach(var schema in updatedCategory.Schemas)
        {
            if(!currentCategory.Schemas.Contains(schema.Name))
                continue;

            var currentFunctions = currentCategory.Schemas.GetByName(schema.Name).Functions;
            if(schema.Functions == null || currentFunctions == null)
                continue;

            foreach(var func in schema.Functions)
            {
                if(!currentFunctions.Contains(func.N))
                    continue;

                var currentFunc = currentFunctions.GetByName(func.N);
                func.JL = currentFunc.JL;
                CarryOverProps(currentFunc.Par, func.Par);
                if(currentFunc.En != null && func.En != null)
                    CarryOverEntity(currentFunc.En, func.En);
            }
        }
    }

    private static void CarryOverEntity(Entity current, Entity updated)
    {
        updated.JL = current.JL;
        CarryOverProps(current.Ps, updated.Ps);

        if(current.NPs == null || updated.NPs == null)
            return;

        foreach(var np in updated.NPs)
        {
            if(np.N != null && current.NPs.Contains(np.N))
                np.JN = current.NPs.GetByName(np.N).JN;
        }
    }

    private static void CarryOverProps(PropertyCollection current, PropertyCollection updated)
    {
        if(current == null || updated == null)
            return;

        foreach(var p in updated)
        {
            if(p.N != null && current.Contains(p.N))
                p.JN = current.GetByName(p.N).JN;
        }
    }
    #endregion
}

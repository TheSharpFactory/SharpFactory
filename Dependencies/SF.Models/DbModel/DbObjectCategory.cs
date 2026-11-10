#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class DbObjectCategory : ModelBase
{

    private string _name = null;
    private DbObjectType _categoryType = DbObjectType.None;
    private DbSchemaCollection _schemas = new DbSchemaCollection();
    public string Name
    {
        get
        {
            return _name;
        }
        set
        {
            if(_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }
    public DbObjectType CategoryType
    {
        get
        {
            return _categoryType;
        }
        set
        {
            if(_categoryType != value)
            {
                _categoryType = value;
                OnPropertyChanged(nameof(CategoryType));
            }
        }
    }
    public DbSchemaCollection Schemas
    {
        get
        {
            return _schemas;
        }
        set
        {
            if(_schemas != value)
            {
                _schemas = value;
                OnPropertyChanged(nameof(Schemas));
            }
        }
    }

    public int Count
    {
        get
        {
            var count = 0;

            foreach(var schema in _schemas)
                count += schema.Count;

            return count;
        }
    }
}

public sealed class DBObjectCategoryCollection : ObservableCollection<DbObjectCategory>
{
    private readonly Dictionary<string, DbObjectCategory> _items = new Dictionary<string, DbObjectCategory>();

    /// <summary>
    /// The objects of the database that were left out because their names are not supported, one line each, for
    /// example "Tables: Sales's.O'Brien (the schema name has characters that are not supported)".
    /// </summary>
    public List<string> NotSupported { get; } = new List<string>();
    protected override void InsertItem(int index, DbObjectCategory item)
    {
        if(_items.ContainsKey(item.Name.ToUpper()))
            throw new ArgumentException($"Object named: {item.Name} already exists.");

        _items.Add(item.Name.ToUpper(), item);
        base.InsertItem(index, item);
    }
    protected override void RemoveItem(int index)
    {
        _items.Remove(this[index].Name.ToUpper());
        base.RemoveItem(index);
    }
    protected override void ClearItems()
    {
        _items.Clear();
        base.ClearItems();
    }
    public bool Contains(string objectName)
    {
        return _items.ContainsKey(objectName.ToUpper());
    }
    public DbObjectCategory GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

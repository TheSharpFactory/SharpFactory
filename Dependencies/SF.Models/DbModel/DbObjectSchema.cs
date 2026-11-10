#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class DbObjectSchema : ModelBase
{
    private string _name = null;
    private DbObjectType _categoryType = DbObjectType.None;
    private bool? _isSelected = false;
    private DbObjectCollection _objects = new DbObjectCollection();
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
    public bool? IsSelected
    {
        get
        {
            return _isSelected;
        }
        set
        {
            if(_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }
    public DbObjectCollection Objects
    {
        get
        {
            return _objects;
        }
        set
        {
            if(_objects != value)
            {
                _objects = value;
                OnPropertyChanged(nameof(Objects));
            }
        }
    }
    public int Count
    {
        get
        {
            return _objects.Count;
        }
    }
}

public sealed class DbSchemaCollection : ObservableCollection<DbObjectSchema>
{
    private readonly Dictionary<string, DbObjectSchema> _items = new Dictionary<string, DbObjectSchema>();
    protected override void InsertItem(int index, DbObjectSchema item)
    {
        if(_items.ContainsKey(item.Name.ToUpper()))
            throw new ArgumentException($"DbSchema named: {item.Name} already exists.");

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
    public bool Contains(string name)
    {
        return _items.ContainsKey(name.ToUpper());
    }
    public DbObjectSchema GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

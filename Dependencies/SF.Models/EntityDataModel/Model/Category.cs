#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class Category : ModelBase
{
    private string _name = null;
    private SchemaCollection _schemas = new SchemaCollection();

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
    public SchemaCollection Schemas
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

    public Category Clone()
    {
        var result = new Category
        {
            Name = _name,
        };

        if(_schemas?.Count > 0)
        {
            foreach(var s in _schemas)
                result.Schemas.Add(s.Clone());
        }

        return result;
    }
}
public sealed class CategoryCollection : ObservableCollection<Category>
{
    private readonly Dictionary<string, Category> _items = new Dictionary<string, Category>();
    protected override void InsertItem(int index, Category item)
    {
        if(_items.ContainsKey(item.Name.ToUpper()))
            throw new ArgumentException($"EntityCategory named: {item.Name} already exists.");

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
    public Category GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

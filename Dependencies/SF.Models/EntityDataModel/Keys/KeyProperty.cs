#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class KeyProperty : ModelBase, IModelMember
{
    private string _name = null;
    //private int _ordinal = -1;

    /// <summary>
    /// Property Name
    /// </summary>
    public string N
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
                OnPropertyChanged(nameof(N));
            }
        }
    }
    //public int Ord
    //{
    //  get
    //  {
    //    return _ordinal;
    //  }
    //  set
    //  {
    //    if(_ordinal != value)
    //    {
    //      _ordinal = value;
    //      OnPropertyChanged(nameof(Ord));
    //    }
    //  }
    //}

    public KeyProperty Clone()
    {
        var result = new KeyProperty
        {
            N = _name,
            //Ord = _ordinal
        };
        return result;
    }
}

public sealed class KeyPropertyCollection : ObservableCollection<KeyProperty>
{
    private readonly Dictionary<string, KeyProperty> _items = new Dictionary<string, KeyProperty>();

    public KeyPropertyCollection() : base()
    {

    }
    public KeyPropertyCollection(List<KeyProperty> items) : base()
    {
        foreach(var item in items)
        {
            Add(item);
        }
    }
    protected override void InsertItem(int index, KeyProperty item)
    {
        if(_items.ContainsKey(item.N.ToUpper()))
            throw new ArgumentException($"KeyProperty named: {item.N} already exists.");

        _items.Add(item.N.ToUpper(), item);
        base.InsertItem(index, item);
    }
    protected override void RemoveItem(int index)
    {
        _items.Remove(this[index].N.ToUpper());
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
    public KeyProperty GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

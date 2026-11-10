#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class Key : ModelBase
{
    private string _name = null;//this is the name of the key in the database   
    private KeyPropertyCollection _keyProperties = null;

    /// <summary>
    /// Key Name
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
    /// <summary>
    /// Properties
    /// </summary>
    public KeyPropertyCollection Ps
    {
        get
        {
            return _keyProperties;
        }
        set
        {
            if(_keyProperties != value)
            {
                _keyProperties = value;
                OnPropertyChanged(nameof(Ps));
            }
        }
    }
    public Key Clone()
    {
        var result = new Key
        {
            N = _name,
        };

        if(_keyProperties?.Count > 0)
        {
            result.Ps = new KeyPropertyCollection();
            foreach(var kp in _keyProperties)
                result.Ps.Add(kp.Clone());
        }

        return result;
    }
}
public sealed class KeyCollection : ObservableCollection<Key>
{
    private readonly Dictionary<string, Key> _items = new Dictionary<string, Key>();
    protected override void InsertItem(int index, Key item)
    {
        if(_items.ContainsKey(item.N.ToUpper()))
            throw new ArgumentException($"EntityKey named: {item.N} already exists.");

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
    public Key GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

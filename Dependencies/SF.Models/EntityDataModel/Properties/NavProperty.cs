#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public enum NavPropertyEnd
{
    None = 0,
    Child = 1,
    Parent = 2,
}
public sealed class NavProperty : ModelBase, IModelMember
{
    private int _fkId = -1;//this is the id of the FK in the model. used to search for the actual FK.
    private string _name = null;//name of the property. plural or singular depending on the multiplicity. also might have a suffix if there is more than one pointing to the same entity.
    private string _multi = null;//this can be Single or Many represented by 1 or *, asterisk being many
    private NavPropertyEnd _end = NavPropertyEnd.None;

    /// <summary>
    /// Id of the FK in the model
    /// </summary>
    public int FKId
    {
        get
        {
            return _fkId;
        }
        set
        {
            if(_fkId != value)
            {
                _fkId = value;
                OnPropertyChanged(nameof(FKId));
            }
        }
    }
    /// <summary>
    /// Nav Property Name
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
    /// Multiplicity this can be Single or Many represented by 1 or *, asterisk being many
    /// </summary>
    public string Mul
    {
        get
        {
            return _multi;
        }
        set
        {
            if(_multi != value)
            {
                _multi = value;
                OnPropertyChanged(nameof(Mul));
            }
        }
    }
    public NavPropertyEnd End
    {
        get
        {
            return _end;
        }
        set
        {
            if(_end != value)
            {
                _end = value;
                OnPropertyChanged(nameof(End));
            }
        }
    }
    public NavProperty Clone()
    {
        var resul = new NavProperty
        {
            FKId = _fkId,
            N = _name,
            Mul = _multi,
            End = _end,
        };
        return resul;
    }
}

public sealed class NavPropertyCollection : ObservableCollection<NavProperty>
{
    private readonly Dictionary<string, NavProperty> _items = new Dictionary<string, NavProperty>();
    private readonly Dictionary<int, NavProperty> _itemsByFKId = new Dictionary<int, NavProperty>();
    protected override void InsertItem(int index, NavProperty item)
    {
        if(_items.ContainsKey(item.N.ToUpper()))
            throw new ArgumentException($"NavProperty named: {item.N} already exists.");
        if(_itemsByFKId.ContainsKey(item.FKId))
            throw new ArgumentException($"NavProperty named: {item.N} already exists for Foreign Key with Id {item.FKId}.");

        _items.Add(item.N.ToUpper(), item);
        _itemsByFKId.Add(item.FKId, item);
        base.InsertItem(index, item);
    }
    protected override void RemoveItem(int index)
    {
        _items.Remove(this[index].N.ToUpper());
        _itemsByFKId.Remove(this[index].FKId);
        base.RemoveItem(index);
    }
    protected override void ClearItems()
    {
        _items.Clear();
        _itemsByFKId.Clear();
        base.ClearItems();
    }
    public bool Contains(string name)
    {
        return _items.ContainsKey(name.ToUpper());
    }
    public NavProperty GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
    public NavProperty GetByFKId(int fkId)
    {
        return _itemsByFKId[fkId];
    }
}

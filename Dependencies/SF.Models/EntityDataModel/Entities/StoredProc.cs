#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class StoredProc : ModelBase, IModelMember
{
    private string _storeName = null;
    private Entity _entity = null;

    //private bool _hasParams = false;
    private PropertyCollection _parameters = null;

    /// <summary>
    /// Stored Proc Name
    /// </summary>
    public string N { get; private set; } = null;
    /// <summary>
    /// Store Name
    /// </summary>
    public string SN
    {
        get
        {
            return _storeName;
        }
        set
        {
            if(_storeName != value)
            {
                _storeName = value;
                if(!string.IsNullOrWhiteSpace(_storeName))
                    N = _storeName.Replace('-', '_').Replace(' ', '_');
                else
                    N = null;
                OnPropertyChanged(nameof(SN));
                OnPropertyChanged(nameof(N));
            }
        }
    }

    /// <summary>
    /// Entity
    /// </summary>
    public Entity En
    {
        get
        {
            return _entity;
        }
        set
        {
            if(_entity != value)
            {
                _entity = value;
                REn = _entity != null;
                OnPropertyChanged(nameof(En));
            }
        }
    }

    /// <summary>
    /// Has Parameters
    /// </summary>
    public bool HPar
    {
        get
        {
            return _parameters?.Count > 0;
        }
    }

    /// <summary>
    /// Returns Entity
    /// </summary>
    public bool REn { get; private set; } = false;

    /// <summary>
    /// Parameters
    /// </summary>
    public PropertyCollection Par
    {
        get
        {
            if(_parameters == null)
                _parameters = new PropertyCollection(this);

            return _parameters;
        }
        set
        {
            if(_parameters != value)
            {
                _parameters = value;
                OnPropertyChanged(nameof(Par));
                OnPropertyChanged(nameof(HPar));
            }
        }
    }
    public bool ShouldSerializePar()
    {
        return _parameters?.Count > 0;
    }
    public int Count
    {
        get
        {
            if(_parameters == null)
                return 0;
            return _parameters.Count;
        }
    }
    public StoredProc Clone()
    {
        var result = new StoredProc
        {
            SN = _storeName,
            En = _entity?.Clone(),
        };

        if(_parameters?.Count > 0)
        {
            result.Par = new PropertyCollection(result);
            foreach(var p in _parameters)
                result.Par.Add(p.Clone());
        }

        return result;
    }
}
public sealed class StoredProcCollection : ObservableCollection<StoredProc>
{
    private readonly Dictionary<string, StoredProc> _items = new Dictionary<string, StoredProc>();
    protected override void InsertItem(int index, StoredProc item)
    {
        if(_items.ContainsKey(item.N.ToUpper()))
            throw new ArgumentException($"StoredProc named: {item.N} already exists.");

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
    public StoredProc GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

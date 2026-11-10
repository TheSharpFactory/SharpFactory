#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class Function : ModelBase, IModelMember
{
    private string _storeName = null;
    private Property _scalar = null;
    private Entity _entity = null;

    //private bool _hasParams = false;
    private PropertyCollection _parameters = null;

    /// <summary>
    /// Function Name
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
    public int Count
    {
        get
        {
            if(_parameters == null)
                return 0;
            return _parameters.Count;
        }
    }

    /// <summary>
    /// Scalar
    /// </summary>
    public Property Sc
    {
        get
        {
            return _scalar;
        }
        set
        {
            if(_scalar != value)
            {
                _scalar = value;
                REn = _scalar == null;
                OnPropertyChanged(nameof(Sc));
                OnPropertyChanged(nameof(REn));
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
                OnPropertyChanged(nameof(REn));
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
    public Function Clone()
    {
        var result = new Function
        {
            SN = _storeName,
            Sc = _scalar != null ? _scalar.Clone() : null,
            En = _entity != null ? _entity.Clone() : null,
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

public sealed class FunctionCollection : ObservableCollection<Function>
{
    private readonly Dictionary<string, Function> _items = new Dictionary<string, Function>();
    protected override void InsertItem(int index, Function item)
    {
        if(_items.ContainsKey(item.N.ToUpper()))
            throw new ArgumentException($"Function named: {item.N} already exists.");

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
    public Function GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

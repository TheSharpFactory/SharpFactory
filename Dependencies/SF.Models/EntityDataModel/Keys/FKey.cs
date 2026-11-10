#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class FKey : ModelBase
{
    private int _id = -1;//this is the id in the model. has NOTHING to do with the database
    private string _name = null;//this is the name of the key in the database   

    private string _parent = null;//this is the unique key table end in the database for example Customer
    private string _parentSchema = null;
    private string _parentKey = null;//this is the name of the PK or UK in the database.
    private string _parentMultiplicity = "1";//this is always 1. because Sql Server does not suport many to many relations unless you use a junction table

    private string _child = null;//this is the foreing key table end in the database for example Orders 
    private string _childSchema = null;
    private string _childMultiplicity = "*";//this can be 1 or *.  by default many thus making it less prone to errors in the database.

    private FKeyPartCollection _fkParts = null;

    /// <summary>
    /// Id of the key in the model
    /// </summary>
    public int Id
    {
        get
        {
            return _id;
        }
        set
        {
            if(_id != value)
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }
    }

    /// <summary>
    /// Name of the key in the database
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
    /// Parent of the relationship. Table name.
    /// </summary>
    public string PT
    {
        get
        {
            return _parent;
        }
        set
        {
            if(_parent != value)
            {
                _parent = value;
                PEn = Utils.GenerateEntityName(_parent);
                OnPropertyChanged(nameof(PT));
                OnPropertyChanged(nameof(PEn));
            }
        }
    }

    /// <summary>
    /// Parent of the relationship. Entity name.
    /// </summary>
    public string PEn { get; private set; } = null;

    /// <summary>
    /// Parent's Schema in the database.
    /// </summary>
    public string PSc
    {
        get
        {
            return _parentSchema;
        }
        set
        {
            if(_parentSchema != value)
            {
                _parentSchema = value;
                OnPropertyChanged(nameof(PSc));
            }
        }
    }

    /// <summary>
    /// Parent Key. This is the Unique Key on the parent table.
    /// </summary>
    public string PKey
    {
        get
        {
            return _parentKey;
        }
        set
        {
            if(_parentKey != value)
            {
                _parentKey = value;
                OnPropertyChanged(nameof(PKey));
            }
        }
    }

    /// <summary>
    /// Parent Multiplicity. This is always 1. Because Sql Server does not suport many to many relations unless you use a junction table.
    /// </summary>
    public string PMu
    {
        get
        {
            return _parentMultiplicity;
        }
        set
        {
            if(_parentMultiplicity != value)
            {
                _parentMultiplicity = value;
                OnPropertyChanged(nameof(PMu));
            }
        }
    }

    /// <summary>
    /// Child of the relationship. Table name.
    /// </summary>
    public string CT
    {
        get
        {
            return _child;
        }
        set
        {
            if(_child != value)
            {
                _child = value;
                CEn = Utils.GenerateEntityName(_child);
                OnPropertyChanged(nameof(CT));
                OnPropertyChanged(nameof(CEn));
            }
        }
    }

    /// <summary>
    /// Child of the relationship. Entity name.
    /// </summary>
    public string CEn { get; private set; } = null;

    /// <summary>
    /// Child's Schema in the database.
    /// </summary>
    public string CSc
    {
        get
        {
            return _childSchema;
        }
        set
        {
            if(_childSchema != value)
            {
                _childSchema = value;
                OnPropertyChanged(nameof(CSc));
            }
        }
    }

    /// <summary>
    /// Child Multiplicity. This can be 1 or *. By default many thus making it less prone to errors in the database.
    /// </summary>
    public string CMu
    {
        get
        {
            return _childMultiplicity;
        }
        set
        {
            if(_childMultiplicity != value)
            {
                _childMultiplicity = value;
                OnPropertyChanged(nameof(CMu));
            }
        }
    }
    public FKeyPartCollection FKParts
    {
        get
        {
            return _fkParts;
        }
        set
        {
            if(_fkParts != value)
            {
                _fkParts = value;
                OnPropertyChanged(nameof(FKParts));
            }
        }
    }

    public FKey Clone()
    {
        var result = new FKey
        {
            Id = _id,//navigation properties point to their foreign key by this id
            N = _name,
            PT = _parent,
            PSc = _parentSchema,
            PKey = _parentKey,
            PMu = _parentMultiplicity,
            CT = _child,
            CSc = _childSchema,
            CMu = _childMultiplicity,
        };

        if(_fkParts?.Count > 0)
        {
            result.FKParts = new FKeyPartCollection();
            foreach(var fkp in _fkParts)
                result.FKParts.Add(fkp.Clone());
        }

        return result;
    }
}
public sealed class FKeyCollection : ObservableCollection<FKey>
{
    private readonly Dictionary<int, FKey> _items = new Dictionary<int, FKey>();
    protected override void InsertItem(int index, FKey item)
    {
        if(_items.ContainsKey(item.Id))
            throw new ArgumentException($"FKey with Id: {item.Id} already exists.");

        _items.Add(item.Id, item);
        base.InsertItem(index, item);
    }
    protected override void RemoveItem(int index)
    {
        _items.Remove(this[index].Id);
        base.RemoveItem(index);
    }
    protected override void ClearItems()
    {
        _items.Clear();
        base.ClearItems();
    }
    public bool Contains(int id)
    {
        return _items.ContainsKey(id);
    }
    public FKey GetById(int id)
    {
        return _items[id];
    }
}

#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class Entity : ModelBase, IModelMember
{
    private string _storeName = null;
    private YesOrNo _hasIdentity = YesOrNo.No;
    private YesOrNo _hasTriggers = YesOrNo.No;
    private Key _primaryKey = null;
    private PropertyCollection _properties = null;
    private NavPropertyCollection _navProperties = null;
    private KeyCollection _uniqueKeys = null;

    /// <summary>
    /// Entity Name
    /// </summary>
    public string N { get; private set; } = null;

    /// <summary>
    /// True if it is required to fully quilify the name in C#
    /// </summary>
    public bool FullyQualify { get; private set; } = false;

    /// <summary>
    /// Has Xml Properties
    /// </summary>
    public bool HXml { get { return Ps?.HXml == true; } }

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
                N = Utils.GenerateEntityName(_storeName);
                OnPropertyChanged(nameof(SN));
                OnPropertyChanged(nameof(N));
            }
        }
    }

    /// <summary>
    /// Has Identity
    /// </summary>
    public YesOrNo HId
    {
        get
        {
            return _hasIdentity;
        }
        set
        {
            if(_hasIdentity != value)
            {
                _hasIdentity = value;
                OnPropertyChanged(nameof(HId));
            }
        }
    }

    /// <summary>
    /// Has Triggers
    /// </summary>
    public YesOrNo HTr
    {
        get
        {
            return _hasTriggers;
        }
        set
        {
            if(_hasTriggers != value)
            {
                _hasTriggers = value;
                OnPropertyChanged(nameof(HTr));
            }
        }
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Key PK
    {
        get
        {
            return _primaryKey;
        }
        set
        {
            if(_primaryKey != value)
            {
                _primaryKey = value;
                OnPropertyChanged(nameof(PK));
            }
        }
    }

    /// <summary>
    /// Properties
    /// </summary>
    public PropertyCollection Ps
    {
        get
        {
            if(_properties == null)
                _properties = new PropertyCollection(this);

            return _properties;
        }
        set
        {
            if(_properties != value)
            {
                _properties = value;
                OnPropertyChanged(nameof(Ps));
            }
        }
    }
    //public bool ShouldSerializePs()
    //{
    //  return _properties?.Count > 0;
    //}

    /// <summary>
    /// Navigation Properties
    /// </summary>
    public NavPropertyCollection NPs
    {
        get
        {
            return _navProperties;
        }
        set
        {
            if(_navProperties != value)
            {
                _navProperties = value;
                OnPropertyChanged(nameof(NPs));
            }
        }
    }

    /// <summary>
    /// Unique Keys
    /// </summary>
    public KeyCollection UKs
    {
        get
        {
            return _uniqueKeys;
        }
        set
        {
            if(_uniqueKeys != value)
            {
                _uniqueKeys = value;
                OnPropertyChanged(nameof(UKs));
            }
        }
    }

    public int Count
    {
        get
        {
            if(_properties == null)
                return 0;
            return _properties.Count;
        }
    }

    /// <summary>
    /// Set FullyQualify to true
    /// </summary>
    internal void RequireFullyQualify()
    {
        FullyQualify = true;
    }

    public Entity Clone()
    {
        var result = new Entity
        {
            SN = _storeName,
            HId = _hasIdentity,
            HTr = _hasTriggers
        };

        if(_primaryKey != null)
            result.PK = _primaryKey.Clone();

        if(_properties?.Count > 0)
        {
            result.Ps = new PropertyCollection(result);
            foreach(var p in _properties)
                result.Ps.Add(p.Clone());
        }

        if(_navProperties?.Count > 0)
        {
            result.NPs = new NavPropertyCollection();
            foreach(var np in _navProperties)
                result.NPs.Add(np.Clone());
        }

        if(_uniqueKeys?.Count > 0)
        {
            result.UKs = new KeyCollection();
            foreach(var uk in _uniqueKeys)
                result.UKs.Add(uk.Clone());
        }

        return result;
    }
}
public sealed class EntityCollection : ObservableCollection<Entity>
{
    private string _schemaName = null;

    private readonly Dictionary<string, Entity> _items = new Dictionary<string, Entity>();

    public void SetSchemaName(string schemaName)
    {
        _schemaName = schemaName;

        if(Count < 1)
            return;

        foreach(var item in this)
        {
            if(string.CompareOrdinal(_schemaName, item.N) == 0)
                item.RequireFullyQualify();
        }
    }
    protected override void InsertItem(int index, Entity item)
    {
        //an entity whose name cannot become a C# name has a null N: it is kept (so the model still opens and can be
        //updated) but not indexed by name; ModelNameCheck reports it before any generation
        if(item.N != null && _items.ContainsKey(item.N.ToUpper()))
            throw new ArgumentException($"Entity named: {item.N} already exists.");

        if(string.CompareOrdinal(_schemaName, item.N) == 0)
            item.RequireFullyQualify();

        if(item.N != null)
            _items.Add(item.N.ToUpper(), item);

        base.InsertItem(index, item);
    }
    protected override void RemoveItem(int index)
    {
        if(this[index].N != null)
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
        if(name == null)
            return false;

        return _items.ContainsKey(name.ToUpper());
    }
    public Entity GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

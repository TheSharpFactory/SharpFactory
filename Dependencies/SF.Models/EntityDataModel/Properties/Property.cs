#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

//here are all the mappings from sql type to .net type. https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx
//these are the mapping EF uses for EDM. for mapping sql types to .net types https://msdn.microsoft.com/en-us/library/bb896344(v=vs.110).aspx

//public enum DotNetType
//{
//  None,
//  String,
//  Int16,
//  Int32,
//  Int64,
//  Double,
//  Decimal,
//  Boolean,
//  Guid,//this might require Guid.FlipEndian...... BUT it remains to be seen
//  Byte,//this is a single byte
//  Binary,//this is a byte array
//}
public sealed class Property : ModelBase, IModelMember
{
    private string _storeName = null;
    private int _typeId = -1;
    private string _default = null;
    private YesOrNo _nullable = YesOrNo.No;//using yes or no is good for JSON serialization. it will become 1 or 0 which will reduce the size of the edmj
    private YesOrNo _identity = YesOrNo.No;
    private YesOrNo _computed = YesOrNo.No;
    private YesOrNo _generated = YesOrNo.No;
    private ParamMode? _mode = null;//by default is in parameter.
    private int? _size = null;

    /// <summary>
    /// Property Name
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
                N = Utils.GeneratePropertyName(_storeName);

                OnPropertyChanged(nameof(SN));
                OnPropertyChanged(nameof(N));
            }
        }
    }

    /// <summary>
    /// This is the type id. It is used to find the correct type information in the model Lookup classes
    /// </summary>
    public int TId
    {
        get
        {
            return _typeId;
        }
        set
        {
            if(_typeId != value)
            {
                _typeId = value;
                var typeInfo = Lookup.TypeInfo[_typeId];
                T = typeInfo.NetType;
                ST = typeInfo.StoreType;
                Str = string.CompareOrdinal(T, "string") == 0;
                Udt = string.CompareOrdinal(typeInfo.SqlDbType, "Udt") == 0;
                Xml = string.CompareOrdinal(typeInfo.SqlDbType, "Xml") == 0;
                if(Udt)
                {
                    //TODO: this might need to be adjusted  .NET Core https://docs.microsoft.com/en-us/ef/core/modeling/spatial
                    Geog = string.CompareOrdinal(ST, "geography") == 0;
                    Geom = string.CompareOrdinal(ST, "geometry") == 0;
                }

                OnPropertyChanged(nameof(ST));
                OnPropertyChanged(nameof(T));
                OnPropertyChanged(nameof(TId));
            }
        }
    }

    /// <summary>
    /// Store Type
    /// </summary>
    public string ST { get; private set; } = null;
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

    /// <summary>
    /// Type
    /// </summary>
    public string T { get; private set; } = null;

    /// <summary>
    /// Default
    /// </summary>
    public string Def
    {
        get
        {
            return _default;
        }
        set
        {
            if(_default != value)
            {
                _default = value;
                OnPropertyChanged(nameof(Def));
                OnPropertyChanged(nameof(RO));
            }
        }
    }

    /// <summary>
    /// Is String
    /// </summary>
    public bool Str { get; private set; } = false;

    /// <summary>
    /// Is Udt
    /// </summary>
    public bool Udt { get; private set; } = false;

    /// <summary>
    /// Is Geometry
    /// </summary>
    public bool Geom { get; private set; } = false;

    /// <summary>
    /// Is Geography
    /// </summary>
    public bool Geog { get; private set; } = false;

    /// <summary>
    /// Is Xml
    /// </summary>
    public bool Xml { get; private set; } = false;

    /// <summary>
    /// Is Nullable
    /// </summary>
    public YesOrNo Nul
    {
        get
        {
            return _nullable;
        }
        set
        {
            if(_nullable != value)
            {
                _nullable = value;
                OnPropertyChanged(nameof(Nul));
            }
        }
    }

    /// <summary>
    /// Is Identity
    /// </summary>
    public YesOrNo Ide
    {
        get
        {
            return _identity;
        }
        set
        {
            if(_identity != value)
            {
                _identity = value;
                OnPropertyChanged(nameof(Ide));
                OnPropertyChanged(nameof(RO));
            }
        }
    }

    /// <summary>
    /// Is Computed
    /// </summary>
    public YesOrNo IsComp
    {
        get
        {
            return _computed;
        }
        set
        {
            if(_computed != value)
            {
                _computed = value;
                OnPropertyChanged(nameof(IsComp));
                OnPropertyChanged(nameof(RO));
            }
        }
    }

    /// <summary>
    /// Is Generated Always
    /// </summary>
    public YesOrNo IsGen
    {
        get
        {
            return _generated;
        }
        set
        {
            if(_generated != value)
            {
                _generated = value;
                OnPropertyChanged(nameof(IsGen));
                OnPropertyChanged(nameof(RO));
            }
        }
    }

    /// <summary>
    /// Is Read Only
    /// </summary>
    public YesOrNo RO
    {
        get
        {
            return _identity == YesOrNo.Yes || _computed == YesOrNo.Yes || _generated == YesOrNo.Yes || (_default != null && _default.Contains("()")) ? YesOrNo.Yes : YesOrNo.No;
        }
    }

    /// <summary>
    /// Parameter Mode
    /// </summary>
    public ParamMode? M
    {
        get
        {
            return _mode;
        }
        set
        {
            if(_mode != value)
            {
                _mode = value;
                OnPropertyChanged(nameof(M));
            }
        }
    }

    /// <summary>
    /// Size
    /// </summary>
    public int? S
    {
        get
        {
            return _size;
        }
        set
        {
            if(_size != value)
            {
                _size = value;
                OnPropertyChanged(nameof(S));
            }
        }
    }

    public void FixNameInModel()
    {
        N = "_" + N;
        OnPropertyChanged(nameof(N));
    }

    public Property Clone()
    {
        var result = new Property
        {
            SN = _storeName,
            TId = _typeId,
            //result.Ord = _ordinal;
            Def = _default,
            Nul = _nullable,
            Ide = _identity,
            IsComp = _computed,
            IsGen = _generated,
            M = _mode,
            S = _size
        };

        return result;
    }
}

public sealed class PropertyCollection : ObservableCollection<Property>
{
    private readonly IModelMember _owner = null;

    private readonly Dictionary<string, Property> _items = new Dictionary<string, Property>();
    private readonly Dictionary<string, Property> _itemsByStoreName = new Dictionary<string, Property>();

    /// <summary>
    /// Has Xml Properties
    /// </summary>
    public bool HXml { get; private set; } = false;

    public PropertyCollection(IModelMember owner) : base()
    {
        _owner = owner;
    }

    protected override void InsertItem(int index, Property item)
    {
        if(string.CompareOrdinal(_owner.N, item.N) == 0)
            item.FixNameInModel();

        if(item.N != null && _items.ContainsKey(item.N.ToUpper()))
            throw new ArgumentException($"Property named: {item.N} already exists.");

        if(item.N != null)
            _items.Add(item.N.ToUpper(), item);

        _itemsByStoreName.Add(item.SN.ToUpper(), item);

        if(string.CompareOrdinal(item.T, "XDocument") == 0)
            HXml = true;//set here to true if there is a property that is an XDocument

        base.InsertItem(index, item);
    }
    protected override void RemoveItem(int index)
    {
        if(this[index].N != null)
            _items.Remove(this[index].N.ToUpper());

        _itemsByStoreName.Remove(this[index].SN.ToUpper());
        base.RemoveItem(index);
    }
    protected override void ClearItems()
    {
        _items.Clear();
        _itemsByStoreName.Clear();
        base.ClearItems();
    }
    public bool Contains(string name)
    {
        return _items.ContainsKey(name.ToUpper());
    }
    public Property GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
    public Property GetByStoreName(string storeName)
    {
        return _itemsByStoreName[storeName.ToUpper()];
    }
}

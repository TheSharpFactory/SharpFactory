#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public enum ModelObjectType
{
    None = 0,
    Entities = 1,
    Views = 2,
    StoredProcs = 3,
    Functions = 4,
    Sequences = 5
}
public sealed class Schema : ModelBase
{
    private string _name = null;
    private ModelObjectType _type = ModelObjectType.None;
    private EntityCollection _entities = null;
    private EntityCollection _views = null;
    //private ViewCollection _views = null;
    private StoredProcCollection _storedProcs = null;
    private FunctionCollection _functions = null;
    private SequenceCollection _sequences = null;
    private FKeyCollection _foreignKeys = null;

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
    public ModelObjectType Type
    {
        get
        {
            return _type;
        }
        set
        {
            if(_type != value)
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }
    }
    public int Count
    {
        get
        {
            if(_entities != null)
                return _entities.Count;

            if(_views != null)
                return _views.Count;

            if(_storedProcs != null)
                return _storedProcs.Count;

            if(_functions != null)
                return _functions.Count;

            if(_sequences != null)
                return _sequences.Count;

            return 0;
        }
    }
    public EntityCollection Entities
    {
        get
        {
            return _entities;
        }
        set
        {
            if(_entities != value)
            {
                _entities = value;

                if(_entities != null)
                    _entities.SetSchemaName(_name);

                OnPropertyChanged(nameof(Entities));
            }
        }
    }
    public EntityCollection Views
    {
        get
        {
            return _views;
        }
        set
        {
            if(_views != value)
            {
                _views = value;

                if(_views != null)
                    _views.SetSchemaName(_name);

                OnPropertyChanged(nameof(Views));
            }
        }
    }
    //public ViewCollection Views
    //{
    //  get
    //  {
    //    return _views;
    //  }
    //  set
    //  {
    //    if(_views != value)
    //    {
    //      _views = value;
    //      OnPropertyChanged(nameof(Views));
    //    }
    //  }
    //}
    public StoredProcCollection StoredProcs
    {
        get
        {
            return _storedProcs;
        }
        set
        {
            if(_storedProcs != value)
            {
                _storedProcs = value;
                OnPropertyChanged(nameof(StoredProcs));
            }
        }
    }
    public FunctionCollection Functions
    {
        get
        {
            return _functions;
        }
        set
        {
            if(_functions != value)
            {
                _functions = value;
                OnPropertyChanged(nameof(Functions));
            }
        }
    }
    public SequenceCollection Sequences
    {
        get
        {
            return _sequences;
        }
        set
        {
            if(_sequences != value)
            {
                _sequences = value;
                OnPropertyChanged(nameof(Sequences));
            }
        }
    }
    public FKeyCollection ForeignKeys
    {
        get { return _foreignKeys; }
        set
        {
            if(_foreignKeys != value)
            {
                _foreignKeys = value;
                OnPropertyChanged(nameof(ForeignKeys));
            }
        }
    }
    public Schema Clone()
    {
        var result = new Schema
        {
            Name = _name,
            Type = _type,
        };
        if(_entities?.Count > 0)
        {
            result.Entities = new EntityCollection();
            foreach(var e in _entities)
                result.Entities.Add(e.Clone());
        }
        //if(_views?.Count > 0)
        //{
        //  result.Views = new ViewCollection();
        //  foreach(var v in _views)
        //    result.Views.Add(v.Clone());
        //}
        if(_views?.Count > 0)
        {
            result.Views = new EntityCollection();
            foreach(var v in _views)
                result.Views.Add(v.Clone());
        }

        if(_storedProcs?.Count > 0)
        {
            result.StoredProcs = new StoredProcCollection();
            foreach(var sp in _storedProcs)
                result.StoredProcs.Add(sp.Clone());
        }

        if(_functions?.Count > 0)
        {
            result.Functions = new FunctionCollection();
            foreach(var f in _functions)
                result.Functions.Add(f.Clone());
        }

        if(_sequences?.Count > 0)
        {
            result.Sequences = new SequenceCollection();
            foreach(var s in _sequences)
                result.Sequences.Add(s.Clone());
        }

        if(_foreignKeys?.Count > 0)
        {
            result.ForeignKeys = new FKeyCollection();
            foreach(var fk in _foreignKeys)
                result.ForeignKeys.Add(fk.Clone());
        }

        return result;
    }
}
public sealed class SchemaCollection : ObservableCollection<Schema>
{
    private readonly Dictionary<string, Schema> _items = new Dictionary<string, Schema>();
    protected override void InsertItem(int index, Schema item)
    {
        if(_items.ContainsKey(item.Name.ToUpper()))
            throw new ArgumentException($"Schema named: {item.Name} already exists.");

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
    public Schema GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

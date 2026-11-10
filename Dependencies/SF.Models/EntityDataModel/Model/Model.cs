#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class Model : ModelBase
{
    private string _name = null;
    private YesOrNo _synced = YesOrNo.No;
    private ConnectionInfo _connectionInfo = new ConnectionInfo();
    private ModelLayers _layers = null;
    private CategoryCollection _categories = new CategoryCollection();
    private FKeyCollection _fKeys = new FKeyCollection();

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
    public YesOrNo Synced
    {
        get
        {
            return _synced;
        }
        set
        {
            if(_synced != value)
            {
                _synced = value;
                OnPropertyChanged(nameof(Synced));
            }
        }
    }
    public ConnectionInfo ConInfo
    {
        get
        {
            return _connectionInfo;
        }
        set
        {
            if(_connectionInfo != value)
            {
                _connectionInfo = value;
                OnPropertyChanged(nameof(ConInfo));
            }
        }
    }
    public ModelLayers Layers
    {
        get
        {
            return _layers;
        }
        set
        {
            if(_layers != value)
            {
                _layers = value;
                OnPropertyChanged(nameof(Layers));
            }
        }
    }
    public CategoryCollection Categories
    {
        get { return _categories; }
        set
        {
            if(_categories != value)
            {
                _categories = value;
                OnPropertyChanged(nameof(Categories));
            }
        }
    }
    public FKeyCollection FKeys
    {
        get { return _fKeys; }
        set
        {
            if(_fKeys != value)
            {
                _fKeys = value;
                OnPropertyChanged(nameof(FKeys));
            }
        }
    }

    public int ObjectCount
    {
        get
        {
            var result = 0;
            foreach(var c in Categories)
            {
                foreach(var s in c.Schemas)
                {
                    result += s.Count;
                }
            }

            return result;
        }
    }

    public bool HasEntities
    {
        get
        {
            return Categories.GetByName(CategoryNames.Entities).Schemas.Count > 0;
        }
    }
    public bool HasStoredProcs
    {
        get
        {
            return Categories.GetByName(CategoryNames.Procs).Schemas.Count > 0;
        }
    }
    public bool HasStoredProcEntities
    {
        get
        {
            if(HasStoredProcs)
            {
                var schemas = Categories.GetByName(CategoryNames.Procs).Schemas;
                foreach(var s in schemas)
                {
                    foreach(var p in s.StoredProcs)
                    {
                        if(p.HPar || p.REn)
                            return true;
                    }
                }
            }

            return false;
        }
    }
    public bool HasFunctions
    {
        get
        {
            return Categories.GetByName(CategoryNames.Funcs).Schemas.Count > 0;
        }
    }
    public bool HasViews
    {
        get
        {
            return Categories.GetByName(CategoryNames.Views).Schemas.Count > 0;
        }
    }

    public Model Clone()
    {
        var result = new Model
        {
            Name = _name,
            Synced = _synced,
            ConInfo = _connectionInfo.Clone(),
            Layers = _layers.Clone(),
        };
        if(_categories?.Count > 0)
        {
            foreach(var c in _categories)
                result.Categories.Add(c.Clone());
        }

        if(_fKeys?.Count > 0)
        {
            foreach(var fk in _fKeys)
                result.FKeys.Add(fk.Clone());
        }

        return result;
    }
}

public sealed class ModelCollection : ObservableCollection<Model>
{
    private readonly Dictionary<string, Model> _items = new Dictionary<string, Model>();
    protected override void InsertItem(int index, Model item)
    {
        if(_items.ContainsKey(item.Name.ToUpper()))
            throw new ArgumentException($"Model named: {item.Name} already exists.");

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
    public Model GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public enum DbObjectType
{
    None,
    Table,
    View,
    Proc,
    Func,
    Seq
}
public sealed class DbObject : ModelBase
{
    private string _name = null;
    private string _schemaName = null;
    private DbObjectType _type = DbObjectType.None;
    private FunctionKind _functionKind = FunctionKind.Scalar;//this is used for functions to save a trip to the database.
    private Property _return = null;//this is used for functions to save a round trip to the database.
    private bool? _isSelected = false;

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
    public string SchemaName
    {
        get
        {
            return _schemaName;
        }
        set
        {
            if(_schemaName != value)
            {
                _schemaName = value;
                OnPropertyChanged(nameof(SchemaName));
            }
        }
    }
    public DbObjectType Type
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
    public FunctionKind FunctionKind
    {
        get
        {
            return _functionKind;
        }
        set
        {
            if(_functionKind != value)
            {
                _functionKind = value;
                OnPropertyChanged(nameof(FunctionKind));
            }
        }
    }
    public Property Return
    {
        get
        {
            return _return;
        }
        set
        {
            if(_return != value)
            {
                _return = value;
                OnPropertyChanged(nameof(Return));
            }
        }
    }
    public bool? IsSelected
    {
        get
        {
            return _isSelected;
        }
        set
        {
            if(_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }
}
public sealed class DbObjectCollection : ObservableCollection<DbObject>
{
    private readonly Dictionary<string, DbObject> _items = new Dictionary<string, DbObject>();
    protected override void InsertItem(int index, DbObject item)
    {
        if(_items.ContainsKey(item.Name.ToUpper()))
            throw new ArgumentException($"Object named: {item.Name} already exists.");

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
    public bool Contains(string objectName)
    {
        return _items.ContainsKey(objectName.ToUpper());
    }
    public DbObject GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

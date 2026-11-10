#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class FKeyPart : ModelBase
{
    private string _parentCol = null;//this is the parent table column   
    private string _childCol = null;//this is the child table column
                                    //private int _ordinal = -1;//this is the ordinal of the column pair. seems like sql server only allows matching columns with the SAME ordinal within their PK or UQ.
                                    //When there is no UK or PK on the child side both ordinals still match within the FK Part

    /// <summary>
    /// Parent column. The column in the parent table.
    /// </summary>
    public string PCol
    {
        get
        {
            return _parentCol;
        }
        set
        {
            if(_parentCol != value)
            {
                _parentCol = value;
                OnPropertyChanged(nameof(PCol));
            }
        }
    }
    /// <summary>
    /// Child column. The column in the child table.
    /// </summary>
    public string CCol
    {
        get
        {
            return _childCol;
        }
        set
        {
            if(_childCol != value)
            {
                _childCol = value;
                OnPropertyChanged(nameof(CCol));
            }
        }
    }
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

    public FKeyPart Clone()
    {
        var result = new FKeyPart
        {
            PCol = _parentCol,
            CCol = _childCol,
            //Ord = _ordinal,
        };
        return result;
    }
}

public sealed class FKeyPartCollection : ObservableCollection<FKeyPart>
{
    private readonly Dictionary<string, FKeyPart> _items = new Dictionary<string, FKeyPart>();
    protected override void InsertItem(int index, FKeyPart item)
    {
        //if(item.Ord < 1)
        //  throw new ArgumentException("Ordinal must be greater than zero");
        if(_items.ContainsKey(item.PCol.ToUpper()))
            throw new ArgumentException($"FKeyPart for Pare Column: {item.PCol} already exists.");

        _items.Add(item.PCol.ToUpper(), item);
        base.InsertItem(index, item);
    }
    protected override void RemoveItem(int index)
    {
        _items.Remove(this[index].PCol);
        base.RemoveItem(index);
    }
    protected override void ClearItems()
    {
        _items.Clear();
        base.ClearItems();
    }
    public bool Contains(string parentColumn)
    {
        return _items.ContainsKey(parentColumn.ToUpper());
    }
    public FKeyPart GetByParentColumn(string parentColumn)
    {
        return _items[parentColumn.ToUpper()];
    }
}

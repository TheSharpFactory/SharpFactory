#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.Models;

public sealed class Sequence : ModelBase, IModelMember
{
    private string _storeName = null;
    private Property _scalar = null;

    /// <summary>
    /// Sequence Name
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
                OnPropertyChanged(nameof(Sc));
            }
        }
    }

    public Sequence Clone()
    {
        var result = new Sequence
        {
            SN = _storeName,
            Sc = _scalar != null ? _scalar.Clone() : null,

        };

        return result;
    }
}

public sealed class SequenceCollection : ObservableCollection<Sequence>
{
    private readonly Dictionary<string, Sequence> _items = new Dictionary<string, Sequence>();
    protected override void InsertItem(int index, Sequence item)
    {
        if(_items.ContainsKey(item.N.ToUpper()))
            throw new ArgumentException($"Sequence named: {item.N} already exists.");

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
    public Sequence GetByName(string name)
    {
        return _items[name.ToUpper()];
    }
}

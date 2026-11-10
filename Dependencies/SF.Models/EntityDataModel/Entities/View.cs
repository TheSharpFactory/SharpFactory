#region Usings
#endregion

namespace SF.Models;

//public class View : ModelBase, IModelMember
//{
//  private string _name = null;
//  private string _storeName = null;
//  private YesOrNo _hasTriggers = YesOrNo.No;

//  /// <summary>
//  /// View Name
//  /// </summary>
//  public string N
//  {
//    get
//    {
//      return _name;
//    }
//  }
//  /// <summary>
//  /// Store Name
//  /// </summary>
//  public string SN
//  {
//    get
//    {
//      return _storeName;
//    }
//    set
//    {
//      if (_storeName != value)
//      {
//        _storeName = value;
//        _name = Utils.GenerateEntityName(_storeName);
//        OnPropertyChanged(nameof(SN));
//        OnPropertyChanged(nameof(N));
//      }
//    }
//  }
//  /// <summary>
//  /// Has Triggers
//  /// </summary>
//  public YesOrNo HTr
//  {
//    get
//    {
//      return _hasTriggers;
//    }
//    set
//    {
//      if (_hasTriggers != value)
//      {
//        _hasTriggers = value;
//        OnPropertyChanged(nameof(HTr));
//      }
//    }
//  }
//  //public bool ShouldSerializePs()
//  //{
//  //  return _properties?.Count > 0;
//  //}
//  public View Clone()
//  {
//    var result = new View
//    {
//      SN = _storeName,
//      HTr = _hasTriggers
//    };
//    return result;
//  }

//}
//public class ViewCollection : ObservableCollection<View>
//{
//  private Dictionary<string, View> _items = new Dictionary<string, View>();
//  protected override void InsertItem(int index, View item)
//  {
//    if (_items.ContainsKey(item.N.ToUpper()))
//      throw new ArgumentException($"View named: {item.N} already exists.");

//    _items.Add(item.N.ToUpper(), item);
//    base.InsertItem(index, item);
//  }
//  protected override void RemoveItem(int index)
//  {
//    _items.Remove(this[index].N.ToUpper());
//    base.RemoveItem(index);
//  }
//  protected override void ClearItems()
//  {
//    _items.Clear();
//    base.ClearItems();
//  }
//  public bool Contains(string name)
//  {
//    return _items.ContainsKey(name.ToUpper());
//  }
//  public View GetByName(string name)
//  {
//    return _items[name.ToUpper()];
//  }
//}

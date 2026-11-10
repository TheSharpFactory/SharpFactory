#region Usings
using System.Collections.Generic;
#endregion

namespace SF.Models;

public sealed class DbKey
{
    //private string _name = null;//this is the name of the key in the database   
    //private List<DbKeyProperty> _keyProperties = null;

    /// <summary>
    /// Store Key Name
    /// </summary>
    public string SN
    {
        get; set;
    }

    /// <summary>
    /// Properties
    /// </summary>
    public List<DbKeyProperty> Ps
    {
        get; set;
    }
}

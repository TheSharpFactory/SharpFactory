using System;
using System.Collections.Generic;
using SF.Models;

namespace SF.Utils;

/// <summary>
/// The order of database objects: by name, or by schema and then name, with the invariant culture so it is the
/// same on every machine (the same as the en-US order the app used before).
/// </summary>
internal sealed class DbObjectOrder : IComparer<DbObject>
{
    public static readonly DbObjectOrder ByName = new DbObjectOrder(false);
    public static readonly DbObjectOrder BySchemaThenName = new DbObjectOrder(true);
    private readonly bool _bySchema;

    private DbObjectOrder(bool bySchema)
    {
        _bySchema = bySchema;
    }

    public int Compare(DbObject x, DbObject y)
    {
        if(_bySchema)
        {
            var result = StringComparer.InvariantCulture.Compare(x.SchemaName, y.SchemaName);
            if(result != 0)
                return result;
        }

        return StringComparer.InvariantCulture.Compare(x.Name, y.Name);
    }
}

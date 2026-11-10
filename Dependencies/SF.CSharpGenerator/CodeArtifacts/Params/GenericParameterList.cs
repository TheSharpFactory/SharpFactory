#region Usings
using System.Collections.Generic;
using System.Linq;
#endregion

namespace SF.CodeGeneration;

public sealed class GenericParameterList : List<GenericParameter>
{

    public GenericParameterList() : base()
    {
    }
    public GenericParameterList(int capacity) : base(capacity)
    {
    }
    public GenericParameterList(IEnumerable<GenericParameter> collection) : base(collection)
    {
    }

    public override string ToString()
    {
        if(Count < 1)
            return "";

        var names = new List<string>(Count);
        foreach(var n in this)
            names.Add(n.Name);

        return string.Join(", ", names);
    }
}

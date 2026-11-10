#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void UsingStatements(List<UsingNamespace> usings)
    {
        if(usings == null || usings.Count < 1)
            return;

        var alreadyAdded = new HashSet<string>(usings.Count);

        foreach(var ns in usings)
        {
            var usingId = $"{ns.Name}-{ns.Alias}";
            if(alreadyAdded.Contains(usingId))
                continue;

            alreadyAdded.Add(usingId);
            GenerateUsingStatement(ns);
        }

        _b.AppendLine();
    }
    private static void GenerateUsingStatement(UsingNamespace ns)
    {
        if(!ns.AliasRequired)
        {
            _b.Append("using ").Append(ns.Name).AppendLine(";");
            return;
        }

        _b.Append("using ").Append(ns.Alias).Append(" = ").Append(ns.Name).Append(';');

        _b.AppendLine(ns.AliasComment);
    }
}

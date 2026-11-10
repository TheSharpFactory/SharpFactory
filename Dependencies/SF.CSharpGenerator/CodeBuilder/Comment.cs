#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void Comment(string comment)
    {
        _b.Append(_currentIndent).Append("// ").AppendLine(comment);
    }
    public static void Comment(List<string> comment)
    {
        _b.Append(_currentIndent)
          .AppendLine("/*");
        Indent();

        foreach(var c in comment)
        {
            _b.Append(_currentIndent).AppendLine(c);
        }

        Outdent();
        _b.AppendLine("*/");
    }
}

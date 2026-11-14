#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void Comment(string comment)
    {
        AppendIndent().Append("// ").AppendLine(comment);
    }
    public static void Comment(List<string> comment)
    {
        AppendIndent()
          .AppendLine("/*");
        Indent();

        foreach(var c in comment)
        {
            AppendIndent().AppendLine(c);
        }

        Outdent();
        //closed at the same indent it was opened
        AppendIndent().AppendLine("*/");
    }
}

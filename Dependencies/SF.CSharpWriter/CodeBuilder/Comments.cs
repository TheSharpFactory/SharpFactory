#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

//comments and regions, at the current indentation
public static partial class CSharpFile
{
    /// <summary>
    /// <c>// comment</c>
    /// </summary>
    public static void Comment(string comment)
    {
        AppendIndent().Append("// ").AppendLine(comment);
    }

    /// <summary>
    /// A block comment: <c>/*</c>, the lines one level deeper, <c>*/</c> at the indentation it was opened at.
    /// </summary>
    public static void Comment(IReadOnlyList<string> lines)
    {
        AppendIndent().AppendLine("/*");
        Indent();
        for(var i = 0; i < lines.Count; i++)
            AppendIndent().AppendLine(lines[i]);

        Outdent();
        AppendIndent().AppendLine("*/");
    }

    /// <summary>
    /// <c>#region name</c>
    /// </summary>
    public static void OpenRegion(string name)
    {
        AppendIndent().Append("#region ").AppendLine(name);
    }

    /// <summary>
    /// <c>#endregion</c>
    /// </summary>
    public static void CloseRegion()
    {
        AppendIndent().AppendLine("#endregion");
    }
}

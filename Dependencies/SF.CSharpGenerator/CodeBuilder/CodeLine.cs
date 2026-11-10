namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    /// <summary>
    /// <para>Closes line with ';' if ';' or ',' is not the last character.  Adds the line one level deeper of indentation.</para>
    /// </summary>
    /// <param name="line"></param>
    public static void Line(string line)
    {
        LineBase(line);
        _b.AppendLine();
    }

    /// <summary>
    /// <para>Closes line with ';' if ';' or ',' is not the last character.  Adds the line one level deeper of indentation.</para>
    /// </summary>
    /// <param name="line"></param>
    /// <param name="comment"></param>
    public static void Line(string line, string comment)
    {
        LineBase(line);
        _b.Append("// ").AppendLine(comment);
    }
    private static void LineBase(string line)
    {
        _b.Append(_currentIndent).Append(line);
        AddSemicolon(line);
    }
    private static void AddSemicolon(string line)
    {
        if(line.Length < 1)
        {
            _b.Append(';');
            return;
        }

        var lastChar = line[line.Length - 1];
        if(lastChar != ';' && lastChar != ',')
            _b.Append(';');
    }

    /// <summary>
    /// <para>Closes line with ';' if ';' or ',' is not the last character.  Adds the line one level deeper of indentation.</para>
    /// <para>Used when 'if', 'foreach', 'while' do not have brackets and a single statement after.</para>
    /// </summary>
    /// <param name="line"></param>
    public static void LineIndented(string line)
    {
        _b.Append(_baseIndent);
        Line(line);
    }

    /// <summary>
    /// Adds line with no semicolon
    /// </summary>
    /// <param name="line"></param>
    public static void LineNoEnding(string line)
    {
        _b.Append(_currentIndent).AppendLine(line);
    }
    public static void EmptyLine()
    {
        _b.AppendLine();
    }

    public static void CloseLine()
    {
        CloseLine(string.Empty);
    }
    public static void CloseLine(string customEnding)
    {
        if(customEnding.Length > 0)
            _b.Append(customEnding);

        AddSemicolon(customEnding);
        _b.AppendLine();
    }
}

namespace SF.CodeGeneration;

//lines and loose text: a statement per line with the semicolon rule, or text appended as it is
public static partial class CSharpFile
{
    #region Lines
    /// <summary>
    /// A statement on its own line at the current indentation, ended with ';' unless it already ends in ';' or ','.
    /// </summary>
    public static void Line(string line)
    {
        AppendIndent().Append(line);
        EndStatement(line);
    }

    /// <summary>
    /// A statement with a comment after it: <c>line; // comment</c>.
    /// </summary>
    public static void Line(string line, string comment)
    {
        AppendIndent().Append(line);
        AddSemicolon(line);
        _b.Append(" // ").AppendLine(comment);
    }

    /// <summary>
    /// A statement one level deeper than the current indentation: the single statement under an if, a foreach or a
    /// while without braces.
    /// </summary>
    public static void LineIndented(string line)
    {
        AppendIndentStep();
        Line(line);
    }

    /// <summary>
    /// A line at the current indentation, written as given: no semicolon is added.
    /// </summary>
    public static void LineNoEnding(string line)
    {
        AppendIndent().AppendLine(line);
    }

    /// <summary>
    /// An empty line (no indentation).
    /// </summary>
    public static void EmptyLine()
    {
        _b.AppendLine();
    }
    #endregion

    #region Loose Text
    /// <summary>
    /// Appends text where the file is: no indentation, no line break. End the line with CloseLine.
    /// </summary>
    public static void Append(string text)
    {
        _b.Append(text);
    }

    /// <summary>
    /// Appends a character where the file is: no indentation, no line break.
    /// </summary>
    public static void Append(char character)
    {
        _b.Append(character);
    }

    /// <summary>
    /// Appends text after the current indentation, without a line break. End the line with CloseLine.
    /// </summary>
    public static void AppendIndented(string text)
    {
        AppendIndent().Append(text);
    }

    /// <summary>
    /// Ends the line being appended with ';' and a line break.
    /// </summary>
    public static void CloseLine()
    {
        CloseLine(null);
    }

    /// <summary>
    /// Ends the line being appended with <paramref name="ending"/> (")" for a call), then ';' unless the ending already
    /// ends in ';' or ',', then a line break.
    /// </summary>
    public static void CloseLine(string ending)
    {
        _b.Append(ending);
        EndStatement(ending);
    }

    /// <summary>
    /// Replaces every match of <paramref name="oldValue"/> in the file so far, from left to right (a token written
    /// earlier and filled in later).
    /// </summary>
    public static void Replace(string oldValue, string newValue)
    {
        _b.Replace(oldValue, newValue);
    }
    #endregion
}

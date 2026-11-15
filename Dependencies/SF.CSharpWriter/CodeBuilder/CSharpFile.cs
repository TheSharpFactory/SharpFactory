namespace SF.CodeGeneration;

/// <summary>
/// <para>Writes C# source files from code, one file at a time: Start a file, call the methods in the order the code
/// reads, then take the text with ToString, WriteTo or Save.</para>
/// <para>Every method writes at the current indentation and ends its own line. Blocks (OpenBlock, and the Open
/// methods of types and members) indent what follows until they are closed. The text passed in is C# written as
/// given; the writer adds the keywords, the punctuation and the semicolons, with one rule: ';' unless the text already
/// ends in ';' or ','.</para>
/// <para>One buffer is reused for every file, so writing allocates nothing once it is as big as the largest file. It
/// is meant for one thread, writing one file after another.</para>
/// </summary>
public static partial class CSharpFile
{
    private static readonly CodeBuffer _b = new CodeBuffer(100000);

    private static int _indentSize = 4;

    //how many blocks deep the next line is: its spaces are written straight into the file, no indent strings
    private static int _indentLevel;

    #region Starting and Ending a File
    /// <summary>
    /// Starts a new file with <paramref name="indentSize"/> spaces per level; the size is kept for the files after it.
    /// </summary>
    public static void Start(int indentSize)
    {
        _indentSize = indentSize;
        Start();
    }

    /// <summary>
    /// Starts a new file: the text, the indentation and any open namespace block are cleared. The indent size of the
    /// last file is kept (4 spaces until one is given).
    /// </summary>
    public static void Start()
    {
        _b.Length = 0;
        _indentLevel = 0;
        _openNamespaceBlocks = 0;

        //an interpolated argument left over from a file that failed halfway
        _pending.Length = 0;
        _pendingInUse = false;
    }

    /// <summary>
    /// The text of the file. WriteTo and Save give the same text without making a string.
    /// </summary>
    public static new string ToString()
    {
        return _b.ToString();
    }
    #endregion

    #region Shared Helpers
    /// <summary>
    /// Writes the current indentation (the spaces of every open block).
    /// </summary>
    private static CodeBuffer AppendIndent()
    {
        return _b.Append(' ', _indentLevel * _indentSize);
    }

    /// <summary>
    /// Writes one more level of indentation (a single statement under an if, foreach or while without braces).
    /// </summary>
    private static CodeBuffer AppendIndentStep()
    {
        return _b.Append(' ', _indentSize);
    }

    /// <summary>
    /// The current indentation and the modifiers followed by a space (nothing for none).
    /// </summary>
    private static CodeBuffer AppendDeclarationStart(string modifiers)
    {
        AppendIndent();
        if(HasText(modifiers))
            _b.Append(modifiers).Append(' ');

        return _b;
    }

    /// <summary>
    /// A clause of a declaration (the where clauses) on its own line, one level deeper. Nothing when it is empty.
    /// </summary>
    private static void AppendClause(string clause)
    {
        AppendClause(null, clause);
    }

    /// <summary>
    /// A clause of a declaration on its own line, one level deeper, after its prefix (": " for a constructor
    /// initializer). Nothing when the clause is empty.
    /// </summary>
    private static void AppendClause(string prefix, string clause)
    {
        if(!HasText(clause))
            return;

        _b.AppendLine();
        AppendIndent();
        AppendIndentStep().Append(prefix).Append(clause);
    }

    /// <summary>
    /// The semicolon rule: ';' after the caller's text unless it already ends in ';' or ',' (always when it is empty).
    /// </summary>
    private static void AddSemicolon(string text)
    {
        if(string.IsNullOrEmpty(text) || NeedsSemicolon(text[text.Length - 1]))
            _b.Append(';');
    }

    private static bool NeedsSemicolon(char lastChar)
    {
        return lastChar != ';' && lastChar != ',';
    }

    /// <summary>
    /// Ends a statement whose last part is the caller's text: the semicolon rule, then the line break.
    /// </summary>
    private static void EndStatement(string lastText)
    {
        AddSemicolon(lastText);
        _b.AppendLine();
    }

    private static bool HasText(string value)
    {
        return !string.IsNullOrEmpty(value);
    }
    #endregion
}

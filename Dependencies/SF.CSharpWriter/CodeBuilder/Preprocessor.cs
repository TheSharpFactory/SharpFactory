namespace SF.CodeGeneration;

//preprocessor directives, written at the left margin whatever the indentation, the way Visual Studio formats them
//(regions are the exception: OpenRegion and CloseRegion follow the code). #define and #undef have to come before
//anything else in the file, comments and blank lines aside
public static partial class CSharpFile
{
    #region Symbols
    /// <summary>
    /// <c>#define symbol</c>
    /// </summary>
    public static void Define(string symbol)
    {
        Directive("define ", symbol);
    }

    /// <summary>
    /// <c>#undef symbol</c>
    /// </summary>
    public static void Undefine(string symbol)
    {
        Directive("undef ", symbol);
    }
    #endregion

    #region Conditional Compilation
    /// <summary>
    /// <c>#if condition</c> ("DEBUG", "NET10_0_OR_GREATER &amp;&amp; !BROWSER").
    /// </summary>
    public static void IfDirective(string condition)
    {
        Directive("if ", condition);
    }

    /// <summary>
    /// <c>#elif condition</c>
    /// </summary>
    public static void ElifDirective(string condition)
    {
        Directive("elif ", condition);
    }

    /// <summary>
    /// <c>#else</c>
    /// </summary>
    public static void ElseDirective()
    {
        _b.AppendLine("#else");
    }

    /// <summary>
    /// <c>#endif</c>
    /// </summary>
    public static void EndIfDirective()
    {
        _b.AppendLine("#endif");
    }
    #endregion

    #region Nullable and Warnings
    /// <summary>
    /// <c>#nullable setting</c> ("enable", "disable", "restore", "enable warnings", "disable annotations").
    /// </summary>
    public static void Nullable(string setting)
    {
        Directive("nullable ", setting);
    }

    /// <summary>
    /// <c>#pragma warning disable codes</c> ("CS0168, CS0219"); every warning when codes is empty.
    /// </summary>
    public static void PragmaWarningDisable(string codes)
    {
        PragmaWarning("disable", codes);
    }

    /// <summary>
    /// <c>#pragma warning restore codes</c>; every warning when codes is empty.
    /// </summary>
    public static void PragmaWarningRestore(string codes)
    {
        PragmaWarning("restore", codes);
    }

    /// <summary>
    /// <c>#warning message</c>: the compiler reports the message as a warning.
    /// </summary>
    public static void WarningDirective(string message)
    {
        Directive("warning ", message);
    }

    /// <summary>
    /// <c>#error message</c>: the compiler reports the message as an error.
    /// </summary>
    public static void ErrorDirective(string message)
    {
        Directive("error ", message);
    }

    /// <summary>
    /// <c>#line text</c> ("hidden", "default", "200 \"Original.cs\"").
    /// </summary>
    public static void LineDirective(string text)
    {
        Directive("line ", text);
    }
    #endregion

    private static void PragmaWarning(string action, string codes)
    {
        _b.Append("#pragma warning ").Append(action);
        if(HasText(codes))
            _b.Append(' ').Append(codes);

        _b.AppendLine();
    }

    private static void Directive(string keyword, string text)
    {
        _b.Append('#').Append(keyword).AppendLine(text);
    }
}

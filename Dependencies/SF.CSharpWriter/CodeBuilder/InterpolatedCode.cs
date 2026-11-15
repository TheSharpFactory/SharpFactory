#region Usings
using System;
using System.Runtime.CompilerServices;
using System.Text;
#endregion

namespace SF.CodeGeneration;

//the $"..." overloads: the same text as the string overloads of the same name, formatted straight into the file
//without making a string
public static partial class CSharpFile
{
    /// <summary>
    /// The text of an interpolated argument ($"..."), formatted the way the string would have been, waiting to be
    /// copied into the file. It is reused: one line at a time.
    /// </summary>
    private static readonly StringBuilder _pending = new StringBuilder(256);

    /// <summary>
    /// True while an interpolated argument is being formatted and not yet written.
    /// </summary>
    private static bool _pendingInUse;

    /// <summary>
    /// <para>Lets the writer's methods take $"..." without building a string: the compiler formats the argument
    /// straight into the writer (as StringBuilder.Append($"...") does), with the same result as the string.</para>
    /// <para>Callers do not use it by name: CSharpFile.Line($"var x = {value}") picks it on its own.</para>
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct InterpolatedCode
    {
        private StringBuilder.AppendInterpolatedStringHandler _inner;

        public InterpolatedCode(int literalLength, int formattedCount)
        {
            //an argument whose own text writes another interpolated line would mix the two: fail instead
            if(_pendingInUse)
                throw new InvalidOperationException("An interpolated argument of CSharpFile is used inside another one.");

            _pendingInUse = true;
            _pending.Length = 0;
            _inner = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount, _pending);
        }

        public void AppendLiteral(string value)
        {
            _inner.AppendLiteral(value);
        }

        public void AppendFormatted(string value)
        {
            _inner.AppendFormatted(value);
        }

        public void AppendFormatted(string value, int alignment = 0, string format = null)
        {
            _inner.AppendFormatted(value, alignment, format);
        }

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
            _inner.AppendFormatted(value);
        }

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string format = null)
        {
            _inner.AppendFormatted(value, alignment, format);
        }

        public void AppendFormatted<T>(T value)
        {
            _inner.AppendFormatted(value);
        }

        public void AppendFormatted<T>(T value, string format)
        {
            _inner.AppendFormatted(value, format);
        }

        public void AppendFormatted<T>(T value, int alignment)
        {
            _inner.AppendFormatted(value, alignment);
        }

        public void AppendFormatted<T>(T value, int alignment, string format)
        {
            _inner.AppendFormatted(value, alignment, format);
        }

        public void AppendFormatted(object value, int alignment = 0, string format = null)
        {
            _inner.AppendFormatted(value, alignment, format);
        }
    }

    #region Lines
    /// <summary>
    /// Line(string) for $"...": the same text, without the string.
    /// </summary>
    public static void Line(ref InterpolatedCode line)
    {
        AppendIndent();
        WritePending(true);
        _b.AppendLine();
    }

    /// <summary>
    /// Line(string, string) for $"...": the same text, without the string.
    /// </summary>
    public static void Line(ref InterpolatedCode line, string comment)
    {
        AppendIndent();
        WritePending(true);
        _b.Append(" // ").AppendLine(comment);
    }

    public static void LineIndented(ref InterpolatedCode line)
    {
        AppendIndentStep();
        Line(ref line);
    }

    public static void LineNoEnding(ref InterpolatedCode line)
    {
        AppendIndent();
        WritePending(false);
        _b.AppendLine();
    }

    public static void Return(ref InterpolatedCode expression)
    {
        AppendIndent().Append("return ");
        WritePending(true);
        _b.AppendLine();
    }

    public static void ReturnNoEnding(ref InterpolatedCode expression)
    {
        AppendIndent().Append("return ");
        WritePending(false);
        _b.AppendLine();
    }

    public static void ReturnIndented(ref InterpolatedCode expression)
    {
        AppendIndentStep();
        Return(ref expression);
    }

    public static void Append(ref InterpolatedCode text)
    {
        WritePending(false);
    }

    public static void AppendIndented(ref InterpolatedCode text)
    {
        AppendIndent();
        WritePending(false);
    }

    public static void Comment(ref InterpolatedCode comment)
    {
        AppendIndent().Append("// ");
        WritePending(false);
        _b.AppendLine();
    }
    #endregion

    #region Control Flow
    public static void If(ref InterpolatedCode condition)
    {
        AppendIndent().Append("if(");
        WritePending(false);
        _b.AppendLine(")");
    }

    public static void ElseIf(ref InterpolatedCode condition)
    {
        AppendIndent().Append("else if(");
        WritePending(false);
        _b.AppendLine(")");
    }

    public static void While(ref InterpolatedCode condition)
    {
        AppendIndent().Append("while(");
        WritePending(false);
        _b.AppendLine(")");
    }

    public static void Switch(ref InterpolatedCode expression)
    {
        AppendIndent().Append("switch(");
        WritePending(false);
        _b.AppendLine(")");
    }

    public static void Case(ref InterpolatedCode value)
    {
        AppendIndent().Append("case ");
        WritePending(false);
        _b.AppendLine(":");
    }
    #endregion

    #region Statements
    public static void Throw(ref InterpolatedCode expression)
    {
        PendingStatement("throw ");
    }

    public static void YieldReturn(ref InterpolatedCode expression)
    {
        PendingStatement("yield return ");
    }

    public static void UsingDeclaration(ref InterpolatedCode declaration)
    {
        PendingStatement("using ");
    }

    public static void AwaitUsingDeclaration(ref InterpolatedCode declaration)
    {
        PendingStatement("await using ");
    }

    public static void GotoCase(ref InterpolatedCode value)
    {
        PendingStatement("goto case ");
    }

    public static void Goto(ref InterpolatedCode label)
    {
        PendingStatement("goto ");
    }

    public static void Using(ref InterpolatedCode resource)
    {
        PendingHeader("using(", ")");
    }

    public static void AwaitUsing(ref InterpolatedCode resource)
    {
        PendingHeader("await using(", ")");
    }

    public static void Lock(ref InterpolatedCode expression)
    {
        PendingHeader("lock(", ")");
    }

    public static void Catch(ref InterpolatedCode declaration)
    {
        PendingHeader("catch(", ")");
    }

    public static void Fixed(ref InterpolatedCode declaration)
    {
        PendingHeader("fixed(", ")");
    }

    public static void Label(ref InterpolatedCode label)
    {
        PendingHeader(string.Empty, ":");
    }

    /// <summary>
    /// A statement that ends with the formatted argument: the keyword, the argument, the semicolon rule.
    /// </summary>
    private static void PendingStatement(string keyword)
    {
        AppendIndent().Append(keyword);
        WritePending(true);
        _b.AppendLine();
    }

    /// <summary>
    /// A line with the formatted argument between an opening and a closing text: <c>lock(</c> ... <c>)</c>.
    /// </summary>
    private static void PendingHeader(string opening, string closing)
    {
        AppendIndent().Append(opening);
        WritePending(false);
        _b.AppendLine(closing);
    }
    #endregion

    /// <summary>
    /// Copies the formatted argument into the file; with <paramref name="semicolon"/>, ends it with ';' the way
    /// AddSemicolon does for a string (unless it ends in ';' or ',', and always when it is empty).
    /// </summary>
    private static void WritePending(bool semicolon)
    {
        _b.Append(_pending);
        if(semicolon && (_pending.Length == 0 || NeedsSemicolon(_pending[_pending.Length - 1])))
            _b.Append(';');

        _pending.Length = 0;
        _pendingInUse = false;
    }
}

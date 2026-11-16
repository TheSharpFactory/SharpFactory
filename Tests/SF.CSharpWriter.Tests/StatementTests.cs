#region Usings
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// Lines and their semicolons, returns, continue, control flow and the exception helpers. Each test starts a new file
/// and checks the exact text.
/// </summary>
public sealed class StatementTests
{
    #region Lines
    [Theory]
    [InlineData("x = 1", "x = 1;")]
    [InlineData("x = 1;", "x = 1;")]
    [InlineData("a,", "a,")]
    [InlineData("", ";")]
    public void LineEndsWithOneSemicolon(string line, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.Line(line);

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }

    [Fact]
    public void LineOfNullIsAnEmptyStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.Line(null);

        Assert.Equal(Code.Lines(";"), CSharpFile.ToString());
    }

    [Fact]
    public void EveryStatementFollowsTheSameSemicolonRule()
    {
        //the caller's text may already end in ';': it is never doubled, whatever the method
        CSharpFile.Start(4);
        CSharpFile.Return("x;");
        CSharpFile.Throw("x;");
        CSharpFile.YieldReturn("x;");
        CSharpFile.UsingDeclaration("var s = Open();");
        CSharpFile.AwaitUsingDeclaration("var s = Open();");
        CSharpFile.GotoCase("1;");
        CSharpFile.Goto("done;");
        CSharpFile.UsingNamespace("System;");
        CSharpFile.UsingStatic("System.Math;");
        CSharpFile.UsingAlias("Text", "System.String;");
        CSharpFile.GlobalUsing("System;");
        CSharpFile.Field("", "int", "a;");
        CSharpFile.Field("", "int", "a", "1;");
        CSharpFile.Const("", "int", "A", "1;");
        CSharpFile.AutoProperty("", "int", "A", "get;", "1;");
        CSharpFile.ExpressionProperty("", "int", "A", "1;");
        CSharpFile.Accessor("get", "1;");
        CSharpFile.ExpressionMethod("", "int", "A", "", "1;");
        CSharpFile.ExpressionConstructor("", "A", "", "B();");
        CSharpFile.ExpressionIndexer("", "int", "int i", "1;");
        CSharpFile.Event("", "Action", "A;");
        CSharpFile.ExpressionOperator("", "A", "+", "A a", "a;");
        CSharpFile.ExpressionConversion("", "implicit", "int", "A a", "1;");

        var expected = Code.Lines(
            "return x;",
            "throw x;",
            "yield return x;",
            "using var s = Open();",
            "await using var s = Open();",
            "goto case 1;",
            "goto done;",
            "using System;",
            "using static System.Math;",
            "using Text = System.String;",
            "global using System;",
            "int a;",
            "int a = 1;",
            "const int A = 1;",
            "int A { get; } = 1;",
            "int A => 1;",
            "get => 1;",
            "int A() => 1;",
            "A() => B();",
            "int this[int i] => 1;",
            "event Action A;",
            "A operator +(A a) => a;",
            "implicit operator int(A a) => 1;");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void LineWritesItsComment()
    {
        CSharpFile.Start(4);
        CSharpFile.Line("x = 1", "the first");

        Assert.Equal(Code.Lines("x = 1; // the first"), CSharpFile.ToString());
    }

    [Fact]
    public void LineIsWrittenAtTheCurrentIndentation()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Line("x = 1");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("{", "    x = 1;", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void LineIndentedGoesOneLevelDeeper()
    {
        CSharpFile.Start(4);
        CSharpFile.If("ready");
        CSharpFile.LineIndented("Go()");

        Assert.Equal(Code.Lines("if(ready)", "    Go();"), CSharpFile.ToString());
    }

    [Fact]
    public void LineNoEndingAddsNoSemicolon()
    {
        CSharpFile.Start(4);
        CSharpFile.LineNoEnding("var query = source");

        Assert.Equal(Code.Lines("var query = source"), CSharpFile.ToString());
    }

    [Fact]
    public void EmptyLineHasNoIndentation()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.EmptyLine();
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("{", "", "}"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("", "Call();")]
    [InlineData(")", "Call());")]
    [InlineData(";", "Call();")]
    public void CloseLineEndsWhatWasAppended(string ending, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.Append("Call()");
        CSharpFile.CloseLine(ending);

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }

    [Fact]
    public void CloseLineOfNullAddsTheSemicolon()
    {
        CSharpFile.Start(4);
        CSharpFile.Append("x = 1");
        CSharpFile.CloseLine(null);

        Assert.Equal(Code.Lines("x = 1;"), CSharpFile.ToString());
    }

    [Fact]
    public void CloseLineWithoutAnEndingAddsTheSemicolon()
    {
        CSharpFile.Start(4);
        CSharpFile.Append("x = 1");
        CSharpFile.CloseLine();

        Assert.Equal(Code.Lines("x = 1;"), CSharpFile.ToString());
    }

    [Fact]
    public void AppendWritesWithoutIndentationOrEnding()
    {
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.Append("a");
        CSharpFile.Append('b');
        CSharpFile.AppendIndented("c");

        Assert.Equal("ab    c", CSharpFile.ToString());
    }
    #endregion

    #region Return and Continue
    [Theory]
    [InlineData("x", "return x;")]
    [InlineData("x;", "return x;")]
    public void ReturnEndsWithOneSemicolon(string value, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.Return(value);

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }

    [Fact]
    public void ReturnForms()
    {
        CSharpFile.Start(4);
        CSharpFile.Return();
        CSharpFile.ReturnNoEnding("source");
        CSharpFile.ReturnIndented("x");
        CSharpFile.ReturnIndented();
        CSharpFile.Continue();
        CSharpFile.ContinueIndented();

        var expected = Code.Lines("return;", "return source", "    return x;", "    return;", "continue;", "    continue;");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion

    #region Control Flow
    [Fact]
    public void IfElseIfElse()
    {
        CSharpFile.Start(4);
        CSharpFile.If("a > 0");
        CSharpFile.LineIndented("x = 1");
        CSharpFile.ElseIf("a < 0");
        CSharpFile.LineIndented("x = -1");
        CSharpFile.Else();
        CSharpFile.LineIndented("x = 0");

        var expected = Code.Lines("if(a > 0)", "    x = 1;", "else if(a < 0)", "    x = -1;", "else", "    x = 0;");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void WhileWithABlock()
    {
        CSharpFile.Start(4);
        CSharpFile.While("i < 10");
        CSharpFile.OpenBlock();
        CSharpFile.Line("i++");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("while(i < 10)", "{", "    i++;", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void DoWhile()
    {
        CSharpFile.Start(4);
        CSharpFile.Do();
        CSharpFile.OpenBlock();
        CSharpFile.Line("i++");
        CSharpFile.CloseBlock();
        CSharpFile.DoWhile("i < 10");

        Assert.Equal(Code.Lines("do", "{", "    i++;", "}", "while(i < 10);"), CSharpFile.ToString());
    }

    [Fact]
    public void ForEach()
    {
        CSharpFile.Start(4);
        CSharpFile.ForEach("item", "items");
        CSharpFile.LineIndented("Use(item)");

        Assert.Equal(Code.Lines("foreach(var item in items)", "    Use(item);"), CSharpFile.ToString());
    }

    [Fact]
    public void SwitchWithCases()
    {
        CSharpFile.Start(4);
        CSharpFile.Switch("mode");
        CSharpFile.OpenBlock();
        CSharpFile.Case("1");
        CSharpFile.Line("Run()");
        CSharpFile.Break();
        CSharpFile.CaseDefault();
        CSharpFile.Break();
        CSharpFile.CloseBlock();

        var expected = Code.Lines("switch(mode)", "{", "    case 1:", "    Run();", "    break;", "    default:", "    break;", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion

    #region Exceptions
    [Fact]
    public void ExceptionHelpers()
    {
        CSharpFile.Start(4);
        CSharpFile.ThrowNotImplementedException();
        CSharpFile.ThrowNotSupportedException();
        CSharpFile.ThrowArgumentException("\"Bad value\"");

        var expected = Code.Lines(
            "throw new NotImplementedException(\"This feature has not been implemented.\");",
            "throw new NotSupportedException(\"This feature is not supported.\");",
            "throw new ArgumentException(\"Bad value\");");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion

    #region For and ForEach
    [Theory]
    [InlineData("var i = 0", "i < n", "i++", "for(var i = 0; i < n; i++)")]
    [InlineData("", "", "", "for(;;)")]
    [InlineData(null, null, null, "for(;;)")]
    [InlineData("", "i < n", "", "for(; i < n;)")]
    [InlineData("int i = 0, j = n", "i < j", "i++, j--", "for(int i = 0, j = n; i < j; i++, j--)")]
    public void ForLeavesOutEmptyParts(string initializer, string condition, string iterator, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.For(initializer, condition, iterator);

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }

    [Fact]
    public void ForWithABlock()
    {
        CSharpFile.Start(4);
        CSharpFile.For("var i = 0", "i < 10", "i++");
        CSharpFile.OpenBlock();
        CSharpFile.Line("total += i");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("for(var i = 0; i < 10; i++)", "{", "    total += i;", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void ForEachWithATypedVariable()
    {
        CSharpFile.Start(4);
        CSharpFile.ForEach("KeyValuePair<string, int>", "pair", "map");

        Assert.Equal(Code.Lines("foreach(KeyValuePair<string, int> pair in map)"), CSharpFile.ToString());
    }

    [Fact]
    public void ForEachWithADeconstruction()
    {
        CSharpFile.Start(4);
        CSharpFile.ForEach("(key, value)", "map");

        Assert.Equal(Code.Lines("foreach(var (key, value) in map)"), CSharpFile.ToString());
    }

    [Fact]
    public void AwaitForEachForms()
    {
        CSharpFile.Start(4);
        CSharpFile.AwaitForEach("item", "ReadAsync()");
        CSharpFile.AwaitForEach("int", "number", "NumbersAsync()");

        Assert.Equal(Code.Lines("await foreach(var item in ReadAsync())", "await foreach(int number in NumbersAsync())"), CSharpFile.ToString());
    }
    #endregion

    #region Try, Catch, Finally and Throw
    [Fact]
    public void TryCatchFinally()
    {
        CSharpFile.Start(4);
        CSharpFile.Try();
        CSharpFile.OpenBlock();
        CSharpFile.Line("Run()");
        CSharpFile.CloseBlock();
        CSharpFile.Catch("ArgumentNullException");
        CSharpFile.OpenBlock();
        CSharpFile.Rethrow();
        CSharpFile.CloseBlock();
        CSharpFile.Catch("IOException ex", "ex.HResult != 0");
        CSharpFile.OpenBlock();
        CSharpFile.Throw("new InvalidOperationException(\"read\", ex)");
        CSharpFile.CloseBlock();
        CSharpFile.Catch();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlock();
        CSharpFile.Finally();
        CSharpFile.OpenBlock();
        CSharpFile.Line("Close()");
        CSharpFile.CloseBlock();

        var expected = Code.Lines(
            "try",
            "{",
            "    Run();",
            "}",
            "catch(ArgumentNullException)",
            "{",
            "    throw;",
            "}",
            "catch(IOException ex) when(ex.HResult != 0)",
            "{",
            "    throw new InvalidOperationException(\"read\", ex);",
            "}",
            "catch",
            "{",
            "}",
            "finally",
            "{",
            "    Close();",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ThrowIsIndented()
    {
        CSharpFile.Start(4);
        CSharpFile.If("value == null");
        CSharpFile.Indent();
        CSharpFile.Throw("new ArgumentNullException(nameof(value))");
        CSharpFile.Outdent();

        Assert.Equal(Code.Lines("if(value == null)", "    throw new ArgumentNullException(nameof(value));"), CSharpFile.ToString());
    }
    #endregion

    #region Using and Lock
    [Fact]
    public void UsingStatementForms()
    {
        CSharpFile.Start(4);
        CSharpFile.Using("var reader = new StringReader(text)");
        CSharpFile.UsingDeclaration("var stream = new MemoryStream()");
        CSharpFile.AwaitUsing("var other = new MemoryStream()");
        CSharpFile.AwaitUsingDeclaration("var last = new MemoryStream()");

        var expected = Code.Lines(
            "using(var reader = new StringReader(text))",
            "using var stream = new MemoryStream();",
            "await using(var other = new MemoryStream())",
            "await using var last = new MemoryStream();");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void LockWithABlock()
    {
        CSharpFile.Start(4);
        CSharpFile.Lock("_gate");
        CSharpFile.OpenBlock();
        CSharpFile.Line("_count++");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("lock(_gate)", "{", "    _count++;", "}"), CSharpFile.ToString());
    }
    #endregion

    #region Yield
    [Fact]
    public void YieldForms()
    {
        CSharpFile.Start(4);
        CSharpFile.YieldReturn("item");
        CSharpFile.YieldBreak();

        Assert.Equal(Code.Lines("yield return item;", "yield break;"), CSharpFile.ToString());
    }
    #endregion

    #region Case Guards and Jumps
    [Fact]
    public void CaseWhenWritesTheGuard()
    {
        CSharpFile.Start(4);
        CSharpFile.CaseWhen("int number", "number > 0");

        Assert.Equal(Code.Lines("case int number when number > 0:"), CSharpFile.ToString());
    }

    [Fact]
    public void JumpForms()
    {
        CSharpFile.Start(4);
        CSharpFile.GotoCase("2");
        CSharpFile.GotoDefault();
        CSharpFile.Goto("done");
        CSharpFile.Label("done");

        Assert.Equal(Code.Lines("goto case 2;", "goto default;", "goto done;", "done:"), CSharpFile.ToString());
    }

    [Fact]
    public void LabelIsWrittenAtTheCurrentIndentation()
    {
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.Label("retry");

        Assert.Equal(Code.Lines("    retry:"), CSharpFile.ToString());
    }
    #endregion

    #region Checked and Unsafe
    [Fact]
    public void CheckedAndUncheckedBlocks()
    {
        CSharpFile.Start(4);
        CSharpFile.Checked();
        CSharpFile.OpenBlock();
        CSharpFile.Line("total *= 2");
        CSharpFile.CloseBlock();
        CSharpFile.Unchecked();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("checked", "{", "    total *= 2;", "}", "unchecked", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void UnsafeAndFixed()
    {
        CSharpFile.Start(4);
        CSharpFile.Unsafe();
        CSharpFile.OpenBlock();
        CSharpFile.Fixed("byte* first = buffer");
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("unsafe", "{", "    fixed(byte* first = buffer)", "    {", "    }", "}"), CSharpFile.ToString());
    }
    #endregion
}

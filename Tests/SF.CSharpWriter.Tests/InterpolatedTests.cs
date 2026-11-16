#region Usings
using System;
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// The $"..." overloads: the same text as the string overloads, written straight into the file.
/// </summary>
public sealed class InterpolatedTests
{
    [Fact]
    public void LineForms()
    {
        var name = "count";
        var value = 5;
        CSharpFile.Start(4);
        CSharpFile.Line($"var {name} = {value}");
        CSharpFile.Line($"{name}++;");
        CSharpFile.Line($"{name} += 1", "again");
        CSharpFile.LineIndented($"{name}--");
        CSharpFile.LineNoEnding($"var query = {name}");

        var expected = Code.Lines("var count = 5;", "count++;", "count += 1; // again", "    count--;", "var query = count");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ReturnForms()
    {
        var name = "total";
        CSharpFile.Start(4);
        CSharpFile.Return($"{name} * 2");
        CSharpFile.ReturnNoEnding($"{name}");
        CSharpFile.ReturnIndented($"{name}");

        Assert.Equal(Code.Lines("return total * 2;", "return total", "    return total;"), CSharpFile.ToString());
    }

    [Fact]
    public void AppendAndCommentForms()
    {
        var name = "x";
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.Append($"[{name}]");
        CSharpFile.AppendIndented($"<{name}>");
        CSharpFile.CloseLine();
        CSharpFile.Comment($"about {name}");

        Assert.Equal(Code.Lines("[x]    <x>;", "    // about x"), CSharpFile.ToString());
    }

    [Fact]
    public void ControlFlowForms()
    {
        var name = "mode";
        var value = 2;
        CSharpFile.Start(4);
        CSharpFile.If($"{name} == {value}");
        CSharpFile.ElseIf($"{name} > {value}");
        CSharpFile.While($"{name} < {value}");
        CSharpFile.Switch($"{name}");
        CSharpFile.Case($"{value}");

        var expected = Code.Lines("if(mode == 2)", "else if(mode > 2)", "while(mode < 2)", "switch(mode)", "case 2:");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void StatementForms()
    {
        var name = "value";
        var number = 2;
        CSharpFile.Start(4);
        CSharpFile.Throw($"new ArgumentNullException(nameof({name}))");
        CSharpFile.YieldReturn($"{name}");
        CSharpFile.YieldReturn($"{name};");
        CSharpFile.UsingDeclaration($"var reader = Open({name})");
        CSharpFile.AwaitUsingDeclaration($"var reader = Open({name})");
        CSharpFile.GotoCase($"{number}");
        CSharpFile.Goto($"step{number}");

        var expected = Code.Lines(
            "throw new ArgumentNullException(nameof(value));",
            "yield return value;",
            "yield return value;",
            "using var reader = Open(value);",
            "await using var reader = Open(value);",
            "goto case 2;",
            "goto step2;");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void HeaderForms()
    {
        var name = "value";
        var number = 2;
        CSharpFile.Start(4);
        CSharpFile.Using($"var reader = Open({name})");
        CSharpFile.AwaitUsing($"var reader = Open({name})");
        CSharpFile.Lock($"_gate{number}");
        CSharpFile.Catch($"IOException {name}");
        CSharpFile.Fixed($"byte* first = {name}");
        CSharpFile.Label($"step{number}");

        var expected = Code.Lines(
            "using(var reader = Open(value))",
            "await using(var reader = Open(value))",
            "lock(_gate2)",
            "catch(IOException value)",
            "fixed(byte* first = value)",
            "step2:");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void InterpolatedStatementsAreIndented()
    {
        var name = "value";
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.Throw($"new Exception({name})");
        CSharpFile.Lock($"{name}");

        Assert.Equal(Code.Lines("    throw new Exception(value);", "    lock(value)"), CSharpFile.ToString());
    }

    [Fact]
    public void EmptyInterpolationEndsWithASemicolon()
    {
        var nothing = string.Empty;
        CSharpFile.Start(4);
        CSharpFile.Line($"{nothing}");

        Assert.Equal(Code.Lines(";"), CSharpFile.ToString());
    }

    [Fact]
    public void AnInterpolationInsideAnotherIsRefusedAndStartRecovers()
    {
        CSharpFile.Start(4);
        Assert.Throws<InvalidOperationException>(WriteNested);

        CSharpFile.Start(4);
        var name = "ok";
        CSharpFile.Line($"{name}");

        Assert.Equal(Code.Lines("ok;"), CSharpFile.ToString());
    }

    private static void WriteNested()
    {
        CSharpFile.Line($"outer {Inner()}");
    }

    private static string Inner()
    {
        var name = "inner";
        CSharpFile.Line($"{name}");
        return name;
    }
}

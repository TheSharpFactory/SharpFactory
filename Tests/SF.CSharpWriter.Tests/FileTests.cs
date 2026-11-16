#region Usings
using System;
using System.Collections.Generic;
using System.Text;
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// The file as a whole: starting over, indentation, blocks, comments, regions, the namespace, the summary, the footer,
/// Replace and files bigger than the first buffer. Each test starts a new file and checks the exact text.
/// </summary>
public sealed class FileTests
{
    #region Start
    [Fact]
    public void StartClearsTheTextAndTheIndentation()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Line("old");

        CSharpFile.Start(4);
        CSharpFile.Line("new");

        Assert.Equal(Code.Lines("new;"), CSharpFile.ToString());
    }

    [Fact]
    public void StartWithoutASizeKeepsTheLastOne()
    {
        CSharpFile.Start(2);
        CSharpFile.Start();
        CSharpFile.OpenBlock();
        CSharpFile.Line("a");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("{", "  a;", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void ANewFileIsEmpty()
    {
        CSharpFile.Start(4);

        Assert.Equal(string.Empty, CSharpFile.ToString());
    }

    [Fact]
    public void EveryLineEndsInCrlf()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "A");
        CSharpFile.Field("public", "int", "B");
        CSharpFile.CloseType();

        var text = CSharpFile.ToString();
        Assert.Equal(4, CountOf(text, "\r\n"));
        Assert.Equal(4, CountOf(text, "\n"));
    }
    #endregion

    #region Indentation and Blocks
    [Fact]
    public void IndentAndOutdent()
    {
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.Indent();
        CSharpFile.Line("a");
        CSharpFile.Outdent();
        CSharpFile.Line("b");

        Assert.Equal(Code.Lines("        a;", "    b;"), CSharpFile.ToString());
    }

    [Fact]
    public void OutdentNeverGoesPastTheLeftMargin()
    {
        CSharpFile.Start(4);
        CSharpFile.Outdent();
        CSharpFile.Outdent();
        CSharpFile.Line("a");

        Assert.Equal(Code.Lines("a;"), CSharpFile.ToString());
    }

    [Fact]
    public void BlockEndings()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithParenthesis();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithSemicolon();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithComma();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlock("}))");

        Assert.Equal(Code.Lines("{", "});", "{", "};", "{", "},", "{", "}))"), CSharpFile.ToString());
    }

    [Fact]
    public void CloseTypeWithNamespaceClosesTheType()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespace("Written");
        CSharpFile.OpenClass("public", "A");
        CSharpFile.CloseTypeWithNamespace();

        Assert.Equal(Code.Lines("namespace Written;", "public class A", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void CloseTypeWithNamespaceClosesANamespaceBlock()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespaceBlock("Written");
        CSharpFile.OpenClass("public", "A");
        CSharpFile.CloseTypeWithNamespace();

        Assert.Equal(Code.Lines("namespace Written", "{", "    public class A", "    {", "    }", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void CloseTypeWithNamespaceClosesOnlyTheInnermostBlock()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespaceBlock("Written");
        CSharpFile.OpenNamespaceBlock("Models");
        CSharpFile.OpenClass("public", "A");
        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.OpenClass("public", "B");
        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.OpenClass("public", "C");
        CSharpFile.CloseTypeWithNamespace();

        var expected = Code.Lines(
            "namespace Written",
            "{",
            "    namespace Models",
            "    {",
            "        public class A",
            "        {",
            "        }",
            "    }",
            "    public class B",
            "    {",
            "    }",
            "}",
            "public class C",
            "{",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ANewFileForgetsTheNamespaceBlocksOfTheLastOne()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespaceBlock("Unfinished");

        CSharpFile.Start(4);
        CSharpFile.OpenNamespace("Written");
        CSharpFile.OpenClass("public", "A");
        CSharpFile.CloseTypeWithNamespace();

        Assert.Equal(Code.Lines("namespace Written;", "public class A", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void CloseNamespaceBlockAfterCloseTypeWithNamespaceIsNotCountedTwice()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespaceBlock("Written");
        CSharpFile.CloseNamespaceBlock();
        CSharpFile.OpenClass("public", "A");
        CSharpFile.CloseTypeWithNamespace();

        Assert.Equal(Code.Lines("namespace Written", "{", "}", "public class A", "{", "}"), CSharpFile.ToString());
    }
    #endregion

    #region Comments, Regions and the Namespace
    [Fact]
    public void CommentIsIndented()
    {
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.Comment("note");

        Assert.Equal(Code.Lines("    // note"), CSharpFile.ToString());
    }

    [Fact]
    public void BlockCommentClosesAtItsOpeningIndentation()
    {
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.Comment(new List<string> { "first", "second" });

        Assert.Equal(Code.Lines("    /*", "        first", "        second", "    */"), CSharpFile.ToString());
    }

    [Fact]
    public void BlockCommentAndFooterTakeAnyList()
    {
        CSharpFile.Start(4);
        CSharpFile.Comment(new[] { "one" });
        CSharpFile.Footer(new[] { "two" });

        var expected = Code.Lines(
            "/*",
            "    one",
            "*/",
            "/************************************************",
            "two",
            "************************************************/");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void RegionOpensAndCloses()
    {
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.OpenRegion("Fields");
        CSharpFile.CloseRegion();

        Assert.Equal(Code.Lines("    #region Fields", "    #endregion"), CSharpFile.ToString());
    }

    [Fact]
    public void NamespaceIsFileScoped()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespace("Written.Models");

        Assert.Equal(Code.Lines("namespace Written.Models;"), CSharpFile.ToString());
    }

    [Fact]
    public void NamespaceBlockIndentsItsTypes()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespaceBlock("Written.Models");
        CSharpFile.OpenClass("public", "Person");
        CSharpFile.CloseType();
        CSharpFile.CloseNamespaceBlock();

        var expected = Code.Lines("namespace Written.Models", "{", "    public class Person", "    {", "    }", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void NamespaceBlocksNestAndFollowEachOther()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespaceBlock("Written");
        CSharpFile.OpenNamespaceBlock("Models");
        CSharpFile.Record("public", "Person(string Name)");
        CSharpFile.CloseNamespaceBlock();
        CSharpFile.CloseNamespaceBlock();
        CSharpFile.OpenNamespaceBlock("Written.Other");
        CSharpFile.CloseNamespaceBlock();

        var expected = Code.Lines(
            "namespace Written",
            "{",
            "    namespace Models",
            "    {",
            "        public record Person(string Name);",
            "    }",
            "}",
            "namespace Written.Other",
            "{",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion

    #region Summary and Footer
    [Fact]
    public void SummaryIsThreeLinesAtTheIndentation()
    {
        CSharpFile.Start(4);
        CSharpFile.Indent();
        CSharpFile.XmlSummary("The person.");

        Assert.Equal(Code.Lines("    /// <summary>", "    /// The person.", "    /// </summary>"), CSharpFile.ToString());
    }

    [Fact]
    public void SummaryRightAfterABraceAddsNoBlankLine()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "A");
        CSharpFile.XmlSummary("The key.");
        CSharpFile.Field("public", "int", "Id");
        CSharpFile.CloseType();

        var expected = Code.Lines("public class A", "{", "    /// <summary>", "    /// The key.", "    /// </summary>", "    public int Id;", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptySummaryWritesNothing(string summary)
    {
        CSharpFile.Start(4);
        CSharpFile.XmlSummary(summary);

        Assert.Equal(string.Empty, CSharpFile.ToString());
    }

    [Fact]
    public void FooterWritesItsLinesInABanner()
    {
        CSharpFile.Start(4);
        CSharpFile.Footer(new List<string> { "Written by a tool", "Do not edit" });

        var expected = Code.Lines(
            "/************************************************",
            "Written by a tool",
            "Do not edit",
            "************************************************/");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void EmptyFooterWritesNothing()
    {
        CSharpFile.Start(4);
        CSharpFile.Footer();
        CSharpFile.Footer(new List<string>());

        Assert.Equal(string.Empty, CSharpFile.ToString());
    }
    #endregion

    #region Replace
    [Fact]
    public void ReplaceChangesEveryMatch()
    {
        CSharpFile.Start(4);
        CSharpFile.Line("var TOKEN = TOKEN + 1");
        CSharpFile.Replace("TOKEN", "count");

        Assert.Equal(Code.Lines("var count = count + 1;"), CSharpFile.ToString());
    }

    [Fact]
    public void ReplaceMatchesFromLeftToRight()
    {
        CSharpFile.Start(4);
        CSharpFile.Append("aaaaa");
        CSharpFile.Replace("aa", "b");

        Assert.Equal("aaaaa".Replace("aa", "b"), CSharpFile.ToString());
    }

    [Fact]
    public void ReplaceWithoutAMatchChangesNothing()
    {
        CSharpFile.Start(4);
        CSharpFile.Line("x = 1");
        CSharpFile.Replace("missing", "y");

        Assert.Equal(Code.Lines("x = 1;"), CSharpFile.ToString());
    }

    [Fact]
    public void ReplaceWithNullRemovesTheMatches()
    {
        CSharpFile.Start(4);
        CSharpFile.Append("a-b-c");
        CSharpFile.Replace("-", null);

        Assert.Equal("abc", CSharpFile.ToString());
    }

    [Fact]
    public void ReplaceCanGrowTheFile()
    {
        CSharpFile.Start(4);
        CSharpFile.Append("x");
        CSharpFile.Replace("x", new string('y', 300000));

        Assert.Equal(new string('y', 300000), CSharpFile.ToString());
    }

    [Fact]
    public void ReplaceOfNothingIsRefused()
    {
        CSharpFile.Start(4);

        Assert.Throws<ArgumentException>(ReplaceEmpty);
        Assert.Throws<ArgumentNullException>(ReplaceNull);
    }

    private static void ReplaceEmpty()
    {
        CSharpFile.Replace(string.Empty, "x");
    }

    private static void ReplaceNull()
    {
        CSharpFile.Replace(null, "x");
    }
    #endregion

    #region Big Files
    [Fact]
    public void AFileBiggerThanTheBufferIsWrittenWhole()
    {
        CSharpFile.Start(4);
        var expected = new StringBuilder();
        CSharpFile.OpenClass("public", "Big");
        expected.Append("public class Big\r\n{\r\n");
        for(var i = 0; i < 20000; i++)
        {
            CSharpFile.Field("public", "int", "Field" + i);
            expected.Append("    public int Field").Append(i).Append(";\r\n");
        }

        CSharpFile.CloseType();
        expected.Append("}\r\n");

        Assert.True(expected.Length > 100000);
        Assert.Equal(expected.ToString(), CSharpFile.ToString());
    }

    [Fact]
    public void AFileAfterABigOneStartsEmpty()
    {
        CSharpFile.Start(4);
        for(var i = 0; i < 20000; i++)
            CSharpFile.Field("public", "int", "Field" + i);

        CSharpFile.Start(4);
        CSharpFile.Line("small");

        Assert.Equal(Code.Lines("small;"), CSharpFile.ToString());
    }
    #endregion

    private static int CountOf(string text, string part)
    {
        var count = 0;
        var at = text.IndexOf(part, StringComparison.Ordinal);
        while(at >= 0)
        {
            count++;
            at = text.IndexOf(part, at + part.Length, StringComparison.Ordinal);
        }

        return count;
    }
}

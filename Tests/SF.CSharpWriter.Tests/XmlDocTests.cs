#region Usings
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// The doc comment tags. Each test starts a new file and checks the exact text.
/// </summary>
public sealed class XmlDocTests
{
    [Fact]
    public void NamedTags()
    {
        CSharpFile.Start(4);
        CSharpFile.XmlParam("id", "The key.");
        CSharpFile.XmlTypeParam("T", "The entity.");
        CSharpFile.XmlException("ArgumentException", "When the key is negative.");

        var expected = Code.Lines(
            "/// <param name=\"id\">The key.</param>",
            "/// <typeparam name=\"T\">The entity.</typeparam>",
            "/// <exception cref=\"ArgumentException\">When the key is negative.</exception>");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void TextTags()
    {
        CSharpFile.Start(4);
        CSharpFile.XmlReturns("The entity, or null.");
        CSharpFile.XmlValue("The name.");
        CSharpFile.XmlRemarks("Cached.");
        CSharpFile.XmlExample("Find(1)");

        var expected = Code.Lines(
            "/// <returns>The entity, or null.</returns>",
            "/// <value>The name.</value>",
            "/// <remarks>Cached.</remarks>",
            "/// <example>Find(1)</example>");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ReferenceTags()
    {
        CSharpFile.Start(4);
        CSharpFile.XmlSeeAlso("Customer.Find");
        CSharpFile.XmlInheritDoc();
        CSharpFile.XmlInheritDoc("IEntity.Id");

        var expected = Code.Lines("/// <seealso cref=\"Customer.Find\"/>", "/// <inheritdoc/>", "/// <inheritdoc cref=\"IEntity.Id\"/>");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void TextIsWrittenAsXml()
    {
        CSharpFile.Start(4);
        CSharpFile.XmlReturns("<see langword=\"true\"/> when <paramref name=\"a\"/> &lt; <c>b</c>.");

        Assert.Equal(Code.Lines("/// <returns><see langword=\"true\"/> when <paramref name=\"a\"/> &lt; <c>b</c>.</returns>"), CSharpFile.ToString());
    }

    [Fact]
    public void XmlLineIsWrittenAsGiven()
    {
        CSharpFile.Start(4);
        CSharpFile.XmlLine("<summary>");
        CSharpFile.XmlLine("One line.");
        CSharpFile.XmlLine("</summary>");

        Assert.Equal(Code.Lines("/// <summary>", "/// One line.", "/// </summary>"), CSharpFile.ToString());
    }

    [Fact]
    public void TagsFollowTheIndentation()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Repository");
        CSharpFile.XmlSummary("Finds an entity.");
        CSharpFile.XmlParam("id", "The key.");
        CSharpFile.XmlReturns("The entity.");
        CSharpFile.OpenMethod("public", "object", "Find", "int id");
        CSharpFile.Return("null");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();

        var expected = Code.Lines(
            "public class Repository",
            "{",
            "    /// <summary>",
            "    /// Finds an entity.",
            "    /// </summary>",
            "    /// <param name=\"id\">The key.</param>",
            "    /// <returns>The entity.</returns>",
            "    public object Find(int id)",
            "    {",
            "        return null;",
            "    }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }
}

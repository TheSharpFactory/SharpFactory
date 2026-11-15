namespace SF.CodeGeneration;

//doc comments: the summary and the other tags, at the current indentation
public static partial class CSharpFile
{
    /// <summary>
    /// The summary of what follows, three lines at the current indentation:
    /// <c>/// &lt;summary&gt;</c>, <c>/// summary</c>, <c>/// &lt;/summary&gt;</c>. Nothing when it is empty.
    /// </summary>
    public static void XmlSummary(string summary)
    {
        if(string.IsNullOrWhiteSpace(summary))
            return;

        AppendIndent().AppendLine("/// <summary>");
        AppendIndent().Append("/// ").AppendLine(summary);
        AppendIndent().AppendLine("/// </summary>");
    }

    #region Doc Tags
    //the text of a tag is written as given: it is XML, so it can hold <see cref="..."/>, <c>, <paramref>... and
    //a '<' or '&' meant as text has to come escaped (&lt; &amp;)
    /// <summary>
    /// <c>/// &lt;param name="name"&gt;text&lt;/param&gt;</c>
    /// </summary>
    public static void XmlParam(string name, string text)
    {
        AppendNamedTag("param", "name", name, text);
    }

    /// <summary>
    /// <c>/// &lt;typeparam name="name"&gt;text&lt;/typeparam&gt;</c>
    /// </summary>
    public static void XmlTypeParam(string name, string text)
    {
        AppendNamedTag("typeparam", "name", name, text);
    }

    /// <summary>
    /// <c>/// &lt;returns&gt;text&lt;/returns&gt;</c>
    /// </summary>
    public static void XmlReturns(string text)
    {
        AppendTag("returns", text);
    }

    /// <summary>
    /// <c>/// &lt;value&gt;text&lt;/value&gt;</c>: what a property holds.
    /// </summary>
    public static void XmlValue(string text)
    {
        AppendTag("value", text);
    }

    /// <summary>
    /// <c>/// &lt;remarks&gt;text&lt;/remarks&gt;</c>
    /// </summary>
    public static void XmlRemarks(string text)
    {
        AppendTag("remarks", text);
    }

    /// <summary>
    /// <c>/// &lt;example&gt;text&lt;/example&gt;</c>
    /// </summary>
    public static void XmlExample(string text)
    {
        AppendTag("example", text);
    }

    /// <summary>
    /// <c>/// &lt;exception cref="type"&gt;text&lt;/exception&gt;</c>
    /// </summary>
    public static void XmlException(string type, string text)
    {
        AppendNamedTag("exception", "cref", type, text);
    }

    /// <summary>
    /// <c>/// &lt;seealso cref="cref"/&gt;</c>
    /// </summary>
    public static void XmlSeeAlso(string cref)
    {
        AppendIndent().Append("/// <seealso cref=\"").Append(cref).AppendLine("\"/>");
    }

    /// <summary>
    /// <c>/// &lt;inheritdoc/&gt;</c>: the documentation of the member it overrides or implements.
    /// </summary>
    public static void XmlInheritDoc()
    {
        AppendIndent().AppendLine("/// <inheritdoc/>");
    }

    /// <summary>
    /// <c>/// &lt;inheritdoc cref="cref"/&gt;</c>: the documentation of another member.
    /// </summary>
    public static void XmlInheritDoc(string cref)
    {
        AppendIndent().Append("/// <inheritdoc cref=\"").Append(cref).AppendLine("\"/>");
    }

    /// <summary>
    /// Any line of a doc comment: <c>/// line</c>.
    /// </summary>
    public static void XmlLine(string line)
    {
        AppendIndent().Append("/// ").AppendLine(line);
    }

    private static void AppendTag(string tag, string text)
    {
        AppendIndent().Append("/// <").Append(tag).Append('>').Append(text).Append("</").Append(tag).AppendLine(">");
    }

    private static void AppendNamedTag(string tag, string attribute, string value, string text)
    {
        AppendIndent()
          .Append("/// <")
          .Append(tag)
          .Append(' ')
          .Append(attribute)
          .Append("=\"")
          .Append(value)
          .Append("\">")
          .Append(text)
          .Append("</")
          .Append(tag)
          .AppendLine(">");
    }
    #endregion
}

#region Usings
using System;
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void XmlSummary(string summary)
    {
        if(string.IsNullOrWhiteSpace(summary))
            return;

        XmlSummary(new List<string>(1) { summary });
    }
    private static void XmlSummary(List<string> summary)
    {
        if(!(summary?.Count > 0))
            return;

        _b.AppendLine();
        AppendIndent().AppendLine("/// <summary>");

        foreach(var line in summary)
            AppendIndent().Append("/// <para>").Append(line).AppendLine("</para>");

        AppendIndent().AppendLine("/// </summary>");
    }
    public static void PropXmlComment(PropertyDeclaration prop)
    {
        if(!(prop.Summary?.Count > 0))
            return;

        XmlSummary(prop.Summary);

        //a property documents its value; <returns> is for methods
        AppendIndent().AppendLine("/// <value>");
        AppendIndent().Append("/// ");
        AppendTypeDescription(prop);
        AppendIndent().AppendLine("/// </value>");
    }
    public static void FieldXmlComment(FieldDeclaration field)
    {
        if(!(field.Summary?.Count > 0))
            return;

        XmlSummary(field.Summary);
    }
    public static void MethodXmlComment(MethodDeclaration method)
    {
        if(!(method.Summary?.Count > 0))
            return;

        XmlSummary(method.Summary);

        if(method.Parameters?.Count > 0)
        {
            foreach(var p in method.Parameters)
            {
                AppendIndent()
                  .Append("/// <param name=\"")
                  .Append(p.Name)
                  .Append("\">")
                  .Append(p.Description)
                  .AppendLine("</param>");
            }
        }

        //methods without parameters also get the <returns> comment
        ReturnTypeXmlComment(method);
    }

    public static void ConstructorXmlComment(ConstructorDeclaration method)
    {
        if(!(method.Summary?.Count > 0))
            return;

        XmlSummary(method.Summary);

        if(!(method.Parameters?.Count > 0))
            return;

        foreach(var p in method.Parameters)
        {
            AppendIndent()
              .Append("/// <param name=\"")
              .Append(p.Name)
              .Append("\">")
              .Append(p.Description)
              .AppendLine("</param>");
        }
    }
    private static void ReturnTypeXmlComment(IComplexDeclaration member)
    {
        AppendIndent().AppendLine("/// <returns>");

        AppendIndent().Append("/// ");
        AppendTypeDescription(member);
        AppendIndent().AppendLine("/// </returns>");
    }
    /// <summary>
    /// Ends the line with the member's type in words: its ReturnTypeForComments, or its type with "&lt;" read as
    /// " of " and "&gt;" left out (List&lt;Region&gt; is "List of Region"). Written straight from the type name, in
    /// slices, without building the text first.
    /// </summary>
    private static void AppendTypeDescription(IComplexDeclaration member)
    {
        if(!string.IsNullOrWhiteSpace(member.ReturnTypeForComments))
        {
            _b.AppendLine(member.ReturnTypeForComments);
            return;
        }

        var rest = member.ReturnType.ToString().AsSpan();
        var next = rest.IndexOfAny('<', '>');
        while(next >= 0)
        {
            _b.Append(rest.Slice(0, next));
            if(rest[next] == '<')
                _b.Append(" of ");

            rest = rest.Slice(next + 1);
            next = rest.IndexOfAny('<', '>');
        }

        _b.Append(rest).AppendLine();
    }
}

#region Usings
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
        _b.Append(_currentIndent).AppendLine("/// <summary>");

        foreach(var line in summary)
            _b.Append(_currentIndent).Append("/// <para>").Append(line).AppendLine("</para>");

        _b.Append(_currentIndent).AppendLine("/// </summary>");
    }
    public static void PropXmlComment(PropertyDeclaration prop)
    {
        if(!(prop.Summary?.Count > 0))
            return;

        XmlSummary(prop.Summary);

        //a property documents its value; <returns> is for methods
        _b.Append(_currentIndent).AppendLine("/// <value>");
        _b.Append(_currentIndent).Append("/// ").AppendLine(DescribeType(prop));
        _b.Append(_currentIndent).AppendLine("/// </value>");
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
                _b.Append(_currentIndent)
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
            _b.Append(_currentIndent)
              .Append("/// <param name=\"")
              .Append(p.Name)
              .Append("\">")
              .Append(p.Description)
              .AppendLine("</param>");
        }
    }
    private static void ReturnTypeXmlComment(IComplexDeclaration member)
    {
        _b.Append(_currentIndent).AppendLine("/// <returns>");

        _b.Append(_currentIndent).Append("/// ").AppendLine(DescribeType(member));
        _b.Append(_currentIndent).AppendLine("/// </returns>");
    }
    private static string DescribeType(IComplexDeclaration member)
    {
        if(!string.IsNullOrWhiteSpace(member.ReturnTypeForComments))
            return member.ReturnTypeForComments;

        return member.ReturnType.ToString().Replace("<", " of ").Replace(">", "");
    }
}

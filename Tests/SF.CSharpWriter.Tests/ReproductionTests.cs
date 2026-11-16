#region Usings
using System;
using System.IO;
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// Real files from public repositories, written again with CSharpFile and compared with the original text.
/// </summary>
public sealed class ReproductionTests
{
    #region Newtonsoft.Json JsonPosition.cs
    //https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/JsonPosition.cs (MIT)
    //the original puts a space after if, switch, for and foreach, and has no line break after its last '}':
    //CSharpFile writes "if(" and ends every line, so the original is brought to that style before comparing
    [Fact]
    public void WritesNewtonsoftJsonPosition()
    {
        var original = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", "JsonPosition.cs.txt"));
        var expected = original
          .Replace("if (", "if(")
          .Replace("switch (", "switch(")
          .Replace("foreach (", "foreach(")
          .Replace("for (", "for(") + "\r\n";

        WriteJsonPosition();

        Assert.Equal(expected, CSharpFile.ToString());
    }

    private static void WriteJsonPosition()
    {
        CSharpFile.Start(4);

        CSharpFile.OpenRegion("License");
        CSharpFile.Comment("Copyright (c) 2007 James Newton-King");
        CSharpFile.LineNoEnding("//"); //Comment("") writes "// " with a trailing space
        CSharpFile.Comment("Permission is hereby granted, free of charge, to any person");
        CSharpFile.Comment("obtaining a copy of this software and associated documentation");
        CSharpFile.Comment("files (the \"Software\"), to deal in the Software without");
        CSharpFile.Comment("restriction, including without limitation the rights to use,");
        CSharpFile.Comment("copy, modify, merge, publish, distribute, sublicense, and/or sell");
        CSharpFile.Comment("copies of the Software, and to permit persons to whom the");
        CSharpFile.Comment("Software is furnished to do so, subject to the following");
        CSharpFile.Comment("conditions:");
        CSharpFile.LineNoEnding("//"); //Comment("") writes "// " with a trailing space
        CSharpFile.Comment("The above copyright notice and this permission notice shall be");
        CSharpFile.Comment("included in all copies or substantial portions of the Software.");
        CSharpFile.LineNoEnding("//"); //Comment("") writes "// " with a trailing space
        CSharpFile.Comment("THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND,");
        CSharpFile.Comment("EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES");
        CSharpFile.Comment("OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND");
        CSharpFile.Comment("NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT");
        CSharpFile.Comment("HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,");
        CSharpFile.Comment("WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING");
        CSharpFile.Comment("FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR");
        CSharpFile.Comment("OTHER DEALINGS IN THE SOFTWARE.");
        CSharpFile.CloseRegion();
        CSharpFile.EmptyLine();

        CSharpFile.UsingNamespace("System");
        CSharpFile.UsingNamespace("System.Collections.Generic");
        CSharpFile.UsingNamespace("System.Globalization");
        CSharpFile.UsingNamespace("System.IO");
        CSharpFile.UsingNamespace("System.Text");
        CSharpFile.UsingNamespace("Newtonsoft.Json.Utilities");
        CSharpFile.EmptyLine();

        CSharpFile.OpenNamespaceBlock("Newtonsoft.Json");

        CSharpFile.OpenEnum("internal", "JsonContainerType");
        CSharpFile.EnumMember("None", "0");
        CSharpFile.EnumMember("Object", "1");
        CSharpFile.EnumMember("Array", "2");
        CSharpFile.LineNoEnding("Constructor = 3"); //EnumMember always adds the comma
        CSharpFile.CloseType();
        CSharpFile.EmptyLine();

        CSharpFile.OpenStruct("internal", "JsonPosition");
        CSharpFile.Field("private static readonly", "char[]", "SpecialCharacters", """{ '.', ' ', '\'', '/', '"', '[', ']', '(', ')', '\t', '\n', '\r', '\f', '\b', '\\', '\u0085', '\u2028', '\u2029' }""");
        CSharpFile.EmptyLine();
        CSharpFile.Field("internal", "JsonContainerType", "Type");
        CSharpFile.Field("internal", "int", "Position");
        CSharpFile.Field("internal", "string?", "PropertyName");
        CSharpFile.Field("internal", "bool", "HasIndex");
        CSharpFile.EmptyLine();

        CSharpFile.OpenConstructor("public", "JsonPosition", "JsonContainerType type");
        CSharpFile.Line("Type = type");
        CSharpFile.Line("HasIndex = TypeHasIndex(type)");
        CSharpFile.Line("Position = -1");
        CSharpFile.Line("PropertyName = null");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();

        WriteCalculateLength();
        CSharpFile.EmptyLine();
        WriteWriteTo();
        CSharpFile.EmptyLine();

        CSharpFile.OpenMethod("internal static", "bool", "TypeHasIndex", "JsonContainerType type");
        CSharpFile.Return("(type == JsonContainerType.Array || type == JsonContainerType.Constructor)");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();

        WriteBuildPath();
        CSharpFile.EmptyLine();
        WriteFormatMessage();

        CSharpFile.CloseTypeWithNamespace();
    }

    private static void WriteCalculateLength()
    {
        CSharpFile.OpenMethod("internal", "int", "CalculateLength", "");
        CSharpFile.Switch("Type");
        CSharpFile.OpenBlock();
        CSharpFile.Case("JsonContainerType.Object");
        CSharpFile.Indent();
        CSharpFile.Return("PropertyName!.Length + 5");
        CSharpFile.Outdent();
        CSharpFile.Case("JsonContainerType.Array");
        CSharpFile.Case("JsonContainerType.Constructor");
        CSharpFile.Indent();
        CSharpFile.Return("MathUtils.IntLength((ulong)Position) + 2");
        CSharpFile.Outdent();
        CSharpFile.CaseDefault();
        CSharpFile.Indent();
        CSharpFile.Throw("new ArgumentOutOfRangeException(nameof(Type))");
        CSharpFile.Outdent();
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
    }

    private static void WriteWriteTo()
    {
        CSharpFile.OpenMethod("internal", "void", "WriteTo", "StringBuilder sb, ref StringWriter? writer, ref char[]? buffer");
        CSharpFile.Switch("Type");
        CSharpFile.OpenBlock();
        CSharpFile.Case("JsonContainerType.Object");
        CSharpFile.Indent();
        CSharpFile.Line("string propertyName = PropertyName!");
        CSharpFile.If("propertyName.IndexOfAny(SpecialCharacters) != -1");
        CSharpFile.OpenBlock();
        CSharpFile.Line("sb.Append(@\"['\")");
        CSharpFile.EmptyLine();
        CSharpFile.If("writer == null");
        CSharpFile.OpenBlock();
        CSharpFile.Line("writer = new StringWriter(sb)");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();
        CSharpFile.Line("JavaScriptUtils.WriteEscapedJavaScriptString(writer, propertyName, '\\'', false, JavaScriptUtils.SingleQuoteCharEscapeFlags, StringEscapeHandling.Default, null, ref buffer)");
        CSharpFile.EmptyLine();
        CSharpFile.Line("sb.Append(@\"']\")");
        CSharpFile.CloseBlock();
        CSharpFile.Else();
        CSharpFile.OpenBlock();
        CSharpFile.If("sb.Length > 0");
        CSharpFile.OpenBlock();
        CSharpFile.Line("sb.Append('.')");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();
        CSharpFile.Line("sb.Append(propertyName)");
        CSharpFile.CloseBlock();
        CSharpFile.Break();
        CSharpFile.Outdent();
        CSharpFile.Case("JsonContainerType.Array");
        CSharpFile.Case("JsonContainerType.Constructor");
        CSharpFile.Indent();
        CSharpFile.Line("sb.Append('[')");
        CSharpFile.Line("sb.Append(Position)");
        CSharpFile.Line("sb.Append(']')");
        CSharpFile.Break();
        CSharpFile.Outdent();
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
    }

    private static void WriteBuildPath()
    {
        CSharpFile.OpenMethod("internal static", "string", "BuildPath", "List<JsonPosition> positions, JsonPosition? currentPosition");
        CSharpFile.Line("int capacity = 0");
        CSharpFile.If("positions != null");
        CSharpFile.OpenBlock();
        CSharpFile.For("int i = 0", "i < positions.Count", "i++");
        CSharpFile.OpenBlock();
        CSharpFile.Line("capacity += positions[i].CalculateLength()");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.If("currentPosition != null");
        CSharpFile.OpenBlock();
        CSharpFile.Line("capacity += currentPosition.GetValueOrDefault().CalculateLength()");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();
        CSharpFile.Line("StringBuilder sb = new StringBuilder(capacity)");
        CSharpFile.Line("StringWriter? writer = null");
        CSharpFile.Line("char[]? buffer = null");
        CSharpFile.If("positions != null");
        CSharpFile.OpenBlock();
        CSharpFile.ForEach("JsonPosition", "state", "positions");
        CSharpFile.OpenBlock();
        CSharpFile.Line("state.WriteTo(sb, ref writer, ref buffer)");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.If("currentPosition != null");
        CSharpFile.OpenBlock();
        CSharpFile.Line("currentPosition.GetValueOrDefault().WriteTo(sb, ref writer, ref buffer)");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();
        CSharpFile.Return("sb.ToString()");
        CSharpFile.CloseBlock();
    }

    private static void WriteFormatMessage()
    {
        CSharpFile.OpenMethod("internal static", "string", "FormatMessage", "IJsonLineInfo? lineInfo, string path, string message");
        CSharpFile.Comment("don't add a fullstop and space when message ends with a new line");
        CSharpFile.If("!message.EndsWith(Environment.NewLine, StringComparison.Ordinal)");
        CSharpFile.OpenBlock();
        CSharpFile.Line("message = message.Trim()");
        CSharpFile.EmptyLine();
        CSharpFile.If("!message.EndsWith('.')");
        CSharpFile.OpenBlock();
        CSharpFile.Line("message += \".\"");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();
        CSharpFile.Line("message += \" \"");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();
        CSharpFile.Line("message += \"Path '{0}'\".FormatWith(CultureInfo.InvariantCulture, path)");
        CSharpFile.EmptyLine();
        CSharpFile.If("lineInfo != null && lineInfo.HasLineInfo()");
        CSharpFile.OpenBlock();
        CSharpFile.Line("message += \", line {0}, position {1}\".FormatWith(CultureInfo.InvariantCulture, lineInfo.LineNumber, lineInfo.LinePosition)");
        CSharpFile.CloseBlock();
        CSharpFile.EmptyLine();
        CSharpFile.Line("message += \".\"");
        CSharpFile.EmptyLine();
        CSharpFile.Return("message");
        CSharpFile.CloseBlock();
    }
    #endregion
}

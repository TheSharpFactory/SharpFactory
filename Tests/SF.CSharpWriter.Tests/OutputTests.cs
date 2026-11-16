#region Usings
using System;
using System.IO;
using System.Text;
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// WriteTo and Save: the same text as ToString, in the encoding asked for, to a writer, a stream or a file.
/// </summary>
public sealed class OutputTests
{
    private static readonly string _folder = Path.Combine(AppContext.BaseDirectory, "written");

    #region TextWriter
    [Fact]
    public void WriteToAWriterGivesTheTextOfToString()
    {
        WriteSample();
        var writer = new StringWriter();
        CSharpFile.WriteTo(writer);

        Assert.Equal(CSharpFile.ToString(), writer.ToString());
    }

    [Fact]
    public void WriteToAWriterOfAnEmptyFileWritesNothing()
    {
        CSharpFile.Start(4);
        var writer = new StringWriter();
        CSharpFile.WriteTo(writer);

        Assert.Equal(string.Empty, writer.ToString());
    }

    [Fact]
    public void WriteToAWriterAddsToWhatItHolds()
    {
        WriteSample();
        var writer = new StringWriter();
        writer.Write("// before\r\n");
        CSharpFile.WriteTo(writer);

        Assert.Equal("// before\r\n" + CSharpFile.ToString(), writer.ToString());
    }
    #endregion

    #region Stream
    [Fact]
    public void WriteToAStreamIsUtf8WithoutAByteOrderMark()
    {
        WriteSample();
        var stream = new MemoryStream();
        CSharpFile.WriteTo(stream);

        Assert.Equal(new UTF8Encoding(false).GetBytes(CSharpFile.ToString()), stream.ToArray());
    }

    [Fact]
    public void WriteToAStreamWritesTheByteOrderMarkOfTheEncoding()
    {
        WriteSample();
        var stream = new MemoryStream();
        CSharpFile.WriteTo(stream, new UTF8Encoding(true));

        var bytes = stream.ToArray();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Equal(CSharpFile.ToString(), Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
    }

    [Fact]
    public void WriteToAStreamInAnotherEncoding()
    {
        WriteSample();
        var stream = new MemoryStream();
        CSharpFile.WriteTo(stream, Encoding.Unicode);

        var bytes = stream.ToArray();
        Assert.Equal(new byte[] { 0xFF, 0xFE }, bytes[..2]);
        Assert.Equal(CSharpFile.ToString(), Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2));
    }

    [Fact]
    public void WriteToAStreamKeepsEveryCharacter()
    {
        CSharpFile.Start(4);
        CSharpFile.Const("public", "string", "Greeting", "\"héllo 日本 \U0001F600\"");
        var stream = new MemoryStream();
        CSharpFile.WriteTo(stream);

        Assert.Equal(CSharpFile.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void ASmallFileAfterABigOneWritesOnlyItsOwnBytes()
    {
        WriteBig();
        CSharpFile.WriteTo(new MemoryStream());

        CSharpFile.Start(4);
        CSharpFile.Line("small");
        var stream = new MemoryStream();
        CSharpFile.WriteTo(stream);

        Assert.Equal(Encoding.UTF8.GetBytes(Code.Lines("small;")), stream.ToArray());
    }
    #endregion

    #region Save
    [Fact]
    public void SaveWritesTheFileAsUtf8WithoutAByteOrderMark()
    {
        WriteSample();
        var path = PathOf("Sample.cs");
        CSharpFile.Save(path);

        Assert.Equal(new UTF8Encoding(false).GetBytes(CSharpFile.ToString()), File.ReadAllBytes(path));
    }

    [Fact]
    public void SaveWritesTheByteOrderMarkOfTheEncoding()
    {
        WriteSample();
        var path = PathOf("WithMark.cs");
        CSharpFile.Save(path, new UTF8Encoding(true));

        var bytes = File.ReadAllBytes(path);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Equal(CSharpFile.ToString(), File.ReadAllText(path));
    }

    [Fact]
    public void SaveReplacesALongerFile()
    {
        var path = PathOf("Replaced.cs");
        WriteBig();
        CSharpFile.Save(path);

        CSharpFile.Start(4);
        CSharpFile.Line("small");
        CSharpFile.Save(path);

        Assert.Equal(Code.Lines("small;"), File.ReadAllText(path));
    }

    [Fact]
    public void SaveOfAnEmptyFileMakesAnEmptyFile()
    {
        var path = PathOf("Empty.cs");
        CSharpFile.Start(4);
        CSharpFile.Save(path);

        Assert.Equal(0, new FileInfo(path).Length);
    }

    [Fact]
    public void SaveWritesAFileBiggerThanTheBuffer()
    {
        var path = PathOf("Big.cs");
        WriteBig();
        CSharpFile.Save(path);

        Assert.Equal(CSharpFile.ToString(), File.ReadAllText(path));
    }

    [Fact]
    public void SaveKeepsTheLineEndings()
    {
        WriteSample();
        var path = PathOf("Crlf.cs");
        CSharpFile.Save(path);

        var text = File.ReadAllText(path);
        Assert.Contains("\r\n", text);
        Assert.DoesNotContain("\n", text.Replace("\r\n", string.Empty));
    }

    [Fact]
    public void SaveIntoAMissingFolderFails()
    {
        CSharpFile.Start(4);

        Assert.Throws<DirectoryNotFoundException>(SaveIntoMissingFolder);
    }

    private static void SaveIntoMissingFolder()
    {
        CSharpFile.Save(Path.Combine(_folder, "no such folder", "A.cs"));
    }
    #endregion

    #region Files
    private static string PathOf(string name)
    {
        Directory.CreateDirectory(_folder);
        return Path.Combine(_folder, name);
    }

    private static void WriteSample()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingNamespace("System");
        CSharpFile.OpenNamespace("Written");
        CSharpFile.XmlSummary("A person.");
        CSharpFile.OpenClass("public sealed", "Person");
        CSharpFile.AutoProperty("public", "string", "Name");
        CSharpFile.ExpressionMethod("public override", "string", "ToString", "", "Name");
        CSharpFile.CloseType();
    }

    private static void WriteBig()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Big");
        for(var i = 0; i < 20000; i++)
            CSharpFile.Field("public", "int", "SomeFieldName");

        CSharpFile.CloseType();
    }
    #endregion
}

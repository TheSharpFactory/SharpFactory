#region Usings
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// <para>Memory: once the buffer is as big as the file, writing allocates nothing. Only ToString allocates, and only
/// the string it returns.</para>
/// <para>Each test writes the same file twice: the first time compiles the methods and grows the buffer, the second
/// is measured. Interpolated arguments use string holes: the writer adds nothing to what the runtime itself
/// allocates to format other types.</para>
/// </summary>
public sealed class AllocationTests
{
    private static readonly List<string> _commentLines = new List<string> { "first", "second" };
    private static readonly List<string> _footerLines = new List<string> { "Written by a tool" };
    private static readonly string _savePath = SavePath();

    private static string SavePath()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "written");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "Allocation.cs");
    }

    #region The Writer Allocates Nothing
    [Fact]
    public void LinesAndReturnsAllocateNothing()
    {
        Assert.Equal(0, AllocationOf(WriteLines));
    }

    [Fact]
    public void ControlFlowAllocatesNothing()
    {
        Assert.Equal(0, AllocationOf(WriteControlFlow));
    }

    [Fact]
    public void StatementsAllocateNothing()
    {
        Assert.Equal(0, AllocationOf(WriteStatements));
    }

    [Fact]
    public void TypesAllocateNothing()
    {
        Assert.Equal(0, AllocationOf(WriteTypes));
    }

    [Fact]
    public void MembersAllocateNothing()
    {
        Assert.Equal(0, AllocationOf(WriteMembers));
    }

    [Fact]
    public void FilePartsAllocateNothing()
    {
        Assert.Equal(0, AllocationOf(WriteFileParts));
    }

    [Fact]
    public void InterpolatedArgumentsAllocateNothing()
    {
        Assert.Equal(0, AllocationOf(WriteInterpolated));
    }

    [Fact]
    public void ReplaceAllocatesNothingOnceItsArrayIsBigEnough()
    {
        Assert.Equal(0, AllocationOf(WriteAndReplace));
    }

    [Fact]
    public void ABigFileAllocatesNothingOnceTheBufferHasGrown()
    {
        Assert.Equal(0, AllocationOf(WriteBigFile));
    }

    [Fact]
    public void TheMeasureSeesAnAllocation()
    {
        //the tests above expect zero: this one proves an allocation would be counted
        Assert.True(AllocationOf(WriteConcatenated) > 0);
    }
    #endregion

    #region Output
    [Fact]
    public void WriteToAWriterAllocatesNothing()
    {
        Assert.Equal(0, AllocationOf(WriteBigFileToWriter));
    }

    [Fact]
    public void WriteToAStreamAllocatesNothingOnceTheBytesHaveGrown()
    {
        Assert.Equal(0, AllocationOf(WriteBigFileToStream));
    }

    [Fact]
    public void SaveAllocatesOnlyTheFileHandleWhateverTheSize()
    {
        //opening a file costs the same few objects for a line or for a megabyte: none of it is the text
        var small = AllocationOf(SaveSmallFile);
        var big = AllocationOf(SaveBigFile);

        Assert.InRange(small, 0, 1024);
        Assert.Equal(small, big);
    }

    private static void WriteBigFileToWriter()
    {
        WriteBigFile();
        CSharpFile.WriteTo(TextWriter.Null);
    }

    private static void WriteBigFileToStream()
    {
        WriteBigFile();
        CSharpFile.WriteTo(Stream.Null);
        CSharpFile.WriteTo(Stream.Null, Encoding.Unicode);
    }

    private static void SaveSmallFile()
    {
        CSharpFile.Start(4);
        CSharpFile.Line("small");
        CSharpFile.Save(_savePath);
    }

    private static void SaveBigFile()
    {
        WriteBigFile();
        CSharpFile.Save(_savePath);
    }
    #endregion

    #region ToString
    [Fact]
    public void ToStringAllocatesOnlyTheString()
    {
        WriteMembers();
        CSharpFile.ToString();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var text = CSharpFile.ToString();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        //a string is its characters (2 bytes each) plus a header of about 22 bytes, rounded up to 8
        Assert.InRange(allocated, text.Length * 2L, (text.Length * 2L) + 32);
    }
    #endregion

    /// <summary>
    /// What writing a file allocates the second time: the first call compiles the methods and grows the buffer.
    /// </summary>
    private static long AllocationOf(Action write)
    {
        write();
        var before = GC.GetAllocatedBytesForCurrentThread();
        write();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    #region Files
    private static void WriteLines()
    {
        CSharpFile.Start(4);
        CSharpFile.Line("var x = 1");
        CSharpFile.Line("x++", "comment");
        CSharpFile.LineIndented("x--");
        CSharpFile.LineNoEnding("var query = source");
        CSharpFile.EmptyLine();
        CSharpFile.Append("Call(");
        CSharpFile.Append('x');
        CSharpFile.CloseLine(")");
        CSharpFile.AppendIndented("y = 2");
        CSharpFile.CloseLine();
        CSharpFile.Return("x");
        CSharpFile.Return();
        CSharpFile.ReturnNoEnding("x");
        CSharpFile.ReturnIndented("x");
        CSharpFile.ReturnIndented();
        CSharpFile.Continue();
        CSharpFile.ContinueIndented();
        CSharpFile.Indent();
        CSharpFile.Outdent();
    }

    private static void WriteControlFlow()
    {
        CSharpFile.Start(4);
        CSharpFile.If("a");
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlock();
        CSharpFile.ElseIf("b");
        CSharpFile.Else();
        CSharpFile.While("c");
        CSharpFile.Do();
        CSharpFile.DoWhile("d");
        CSharpFile.Switch("e");
        CSharpFile.Case("1");
        CSharpFile.CaseWhen("int n", "n > 0");
        CSharpFile.CaseDefault();
        CSharpFile.Break();
        CSharpFile.ForEach("item", "items");
        CSharpFile.ForEach("int", "item", "items");
        CSharpFile.For("var i = 0", "i < n", "i++");
        CSharpFile.For("", "", "");
        CSharpFile.AwaitForEach("item", "items");
        CSharpFile.AwaitForEach("int", "item", "items");
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithComma();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithSemicolon();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithParenthesis();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlock("}))");
    }

    private static void WriteStatements()
    {
        CSharpFile.Start(4);
        CSharpFile.Try();
        CSharpFile.Catch();
        CSharpFile.Catch("IOException ex");
        CSharpFile.Catch("IOException ex", "ex.HResult != 0");
        CSharpFile.Finally();
        CSharpFile.Throw("new Exception()");
        CSharpFile.Rethrow();
        CSharpFile.ThrowArgumentException("\"bad\"");
        CSharpFile.ThrowNotImplementedException();
        CSharpFile.ThrowNotSupportedException();
        CSharpFile.Using("var s = Open()");
        CSharpFile.UsingDeclaration("var s = Open()");
        CSharpFile.AwaitUsing("var s = Open()");
        CSharpFile.AwaitUsingDeclaration("var s = Open()");
        CSharpFile.Lock("_gate");
        CSharpFile.YieldReturn("x");
        CSharpFile.YieldBreak();
        CSharpFile.GotoCase("1");
        CSharpFile.GotoDefault();
        CSharpFile.Goto("done");
        CSharpFile.Label("done");
        CSharpFile.Checked();
        CSharpFile.Unchecked();
        CSharpFile.Unsafe();
        CSharpFile.Fixed("byte* p = buffer");
    }

    private static void WriteTypes()
    {
        CSharpFile.Start(4);
        CSharpFile.Attribute("Serializable");
        CSharpFile.OpenClass("public sealed", "Repository<T>", "IRepository<T>", "where T : class");
        CSharpFile.OpenStruct("public readonly", "Point", "IEquatable<Point>");
        CSharpFile.CloseType();
        CSharpFile.OpenInterface("public", "IThing");
        CSharpFile.CloseType();
        CSharpFile.OpenRecord("public", "Person(string Name)", "Entity");
        CSharpFile.CloseType();
        CSharpFile.OpenRecordStruct("public", "Range(int Start, int End)");
        CSharpFile.CloseType();
        CSharpFile.Record("public", "Box<T>(T Value)", null, "where T : class");
        CSharpFile.RecordStruct("public", "Size(int W, int H)");
        CSharpFile.OpenEnum("public", "Color", "byte");
        CSharpFile.EnumMember("Red");
        CSharpFile.EnumMember("Green", "2");
        CSharpFile.CloseType();
        CSharpFile.Delegate("public", "T", "Factory<T>", "int id", "where T : class");
        CSharpFile.CloseTypeWithNamespace();
    }

    private static void WriteMembers()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Customer");
        CSharpFile.Field("private", "int", "_id");
        CSharpFile.Field("private", "int", "_age", "0");
        CSharpFile.Const("public", "int", "Max", "10");
        CSharpFile.AutoProperty("public", "string", "Name");
        CSharpFile.AutoProperty("public", "int", "Id", "get; init;");
        CSharpFile.AutoProperty("public", "int", "Count", "get; set;", "1");
        CSharpFile.ExpressionProperty("public", "int", "Age", "_age");
        CSharpFile.OpenProperty("public", "int", "Level");
        CSharpFile.Get("_age");
        CSharpFile.Set("_age = value");
        CSharpFile.Init("_age = value");
        CSharpFile.Accessor("private set", "_age = value");
        CSharpFile.OpenGet();
        CSharpFile.CloseBlock();
        CSharpFile.OpenSet();
        CSharpFile.CloseBlock();
        CSharpFile.OpenInit();
        CSharpFile.CloseBlock();
        CSharpFile.OpenAccessor("protected set");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.OpenMethod("public", "T", "Find<T>", "int id", "where T : class");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionMethod("public", "int", "Add", "int a, int b", "a + b");
        CSharpFile.MethodSignature("public abstract", "void", "Save", "int id");
        CSharpFile.OpenConstructor("public", "Customer", "int id", "base(id)");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionConstructor("public", "Customer", "string name", "Name = name");
        CSharpFile.OpenFinalizer("Customer");
        CSharpFile.CloseBlock();
        CSharpFile.OpenIndexer("public", "string", "int index");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionIndexer("public", "int", "string name", "0");
        CSharpFile.IndexerSignature("public abstract", "int", "int i", "get;");
        CSharpFile.Event("public", "EventHandler", "Changed");
        CSharpFile.OpenEvent("public", "EventHandler", "Reset");
        CSharpFile.Add("_reset += value");
        CSharpFile.Remove("_reset -= value");
        CSharpFile.OpenAdd();
        CSharpFile.CloseBlock();
        CSharpFile.OpenRemove();
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.OpenOperator("public static", "Money", "-", "Money m");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionOperator("public static", "Money", "+", "Money a, Money b", "a");
        CSharpFile.OperatorSignature("static abstract", "T", "+", "T a, T b");
        CSharpFile.OpenConversion("public static", "explicit", "Money", "decimal d");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionConversion("public static", "implicit", "decimal", "Money m", "m.Amount");
        CSharpFile.CloseType();
    }

    private static void WriteFileParts()
    {
        CSharpFile.Start(4);
        CSharpFile.AutoGeneratedHeader();
        CSharpFile.Define("FEATURE");
        CSharpFile.Undefine("OTHER");
        CSharpFile.UsingNamespace("System");
        CSharpFile.UsingStatic("System.Math");
        CSharpFile.UsingAlias("Text", "System.String");
        CSharpFile.GlobalUsing("System.Linq");
        CSharpFile.Nullable("enable");
        CSharpFile.PragmaWarningDisable("CS0168");
        CSharpFile.PragmaWarningRestore(null);
        CSharpFile.IfDirective("DEBUG");
        CSharpFile.ElifDirective("RELEASE");
        CSharpFile.ElseDirective();
        CSharpFile.EndIfDirective();
        CSharpFile.WarningDirective("check");
        CSharpFile.ErrorDirective("stop");
        CSharpFile.LineDirective("hidden");
        CSharpFile.OpenNamespace("Written");
        CSharpFile.OpenNamespaceBlock("Written.Inner");
        CSharpFile.OpenRegion("Fields");
        CSharpFile.CloseRegion();
        CSharpFile.Comment("note");
        CSharpFile.Comment(_commentLines);
        CSharpFile.XmlSummary("The thing.");
        CSharpFile.XmlParam("id", "The key.");
        CSharpFile.XmlTypeParam("T", "The type.");
        CSharpFile.XmlReturns("The result.");
        CSharpFile.XmlValue("The value.");
        CSharpFile.XmlRemarks("Remarks.");
        CSharpFile.XmlExample("Example.");
        CSharpFile.XmlException("ArgumentException", "When bad.");
        CSharpFile.XmlSeeAlso("Other");
        CSharpFile.XmlInheritDoc();
        CSharpFile.XmlInheritDoc("Other.Id");
        CSharpFile.XmlLine("<para>More.</para>");
        CSharpFile.CloseNamespaceBlock();
        CSharpFile.Footer(_footerLines);
    }

    private static void WriteInterpolated()
    {
        var name = "value";
        var type = "Customer";
        CSharpFile.Start(4);
        CSharpFile.Line($"var {name} = new {type}()");
        CSharpFile.Line($"{name}.Save()", "saved");
        CSharpFile.LineIndented($"{name}++");
        CSharpFile.LineNoEnding($"var q = {name}");
        CSharpFile.Return($"{name}");
        CSharpFile.ReturnNoEnding($"{name}");
        CSharpFile.ReturnIndented($"{name}");
        CSharpFile.Append($"[{name}]");
        CSharpFile.AppendIndented($"<{name}>");
        CSharpFile.CloseLine();
        CSharpFile.Comment($"about {name}");
        CSharpFile.If($"{name} != null");
        CSharpFile.ElseIf($"{name} == {type}");
        CSharpFile.While($"{name} > 0");
        CSharpFile.Switch($"{name}");
        CSharpFile.Case($"{type}");
        CSharpFile.Throw($"new ArgumentException(nameof({name}))");
        CSharpFile.YieldReturn($"{name}");
        CSharpFile.UsingDeclaration($"var r = Open({name})");
        CSharpFile.AwaitUsingDeclaration($"var r = Open({name})");
        CSharpFile.GotoCase($"{type}");
        CSharpFile.Goto($"{name}");
        CSharpFile.Using($"var r = Open({name})");
        CSharpFile.AwaitUsing($"var r = Open({name})");
        CSharpFile.Lock($"{name}");
        CSharpFile.Catch($"{type} ex");
        CSharpFile.Fixed($"byte* p = {name}");
        CSharpFile.Label($"{name}");
    }

    private static void WriteAndReplace()
    {
        CSharpFile.Start(4);
        for(var i = 0; i < 2000; i++)
            CSharpFile.Line("var TOKEN = TOKEN + 1");

        CSharpFile.Replace("TOKEN", "a much longer replacement");
        CSharpFile.Replace("a much longer replacement", "x");
    }

    private static void WriteConcatenated()
    {
        var name = "value";
        CSharpFile.Start(4);
        //the caller builds a string before the writer sees it
        CSharpFile.Line(string.Concat("var ", name, " = 1"));
    }

    private static void WriteBigFile()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Big");
        for(var i = 0; i < 20000; i++)
            CSharpFile.Field("public", "int", "SomeFieldName");

        CSharpFile.CloseType();
    }
    #endregion
}

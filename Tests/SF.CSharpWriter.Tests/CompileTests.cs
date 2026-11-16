#region Usings
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// Whole files written with every declaration, compiled with Roslyn: the text the writer produces is valid C#, at
/// every indent size.
/// </summary>
public sealed class CompileTests
{
    [Fact]
    public void TheCompilerReportsBrokenCode()
    {
        //the other tests expect no errors: this one proves an error would be seen
        Assert.NotEmpty(Compiler.Errors("public class Broken { int }"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void EveryDeclarationCompiles(int indentSize)
    {
        WriteEveryDeclaration(indentSize);

        Assert.Empty(Compiler.Errors(CSharpFile.ToString()));
    }

    [Fact]
    public void StatementsInsideMethodsCompile()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingNamespace("System");
        CSharpFile.UsingNamespace("System.Collections.Generic");
        CSharpFile.OpenNamespace("Written.Logic");
        CSharpFile.OpenClass("public static", "Rules");
        CSharpFile.OpenMethod("public static", "int", "Score", "List<int> values, int mode");
        CSharpFile.Line("var total = 0");
        CSharpFile.ForEach("value", "values");
        CSharpFile.OpenBlock();
        CSharpFile.If("value < 0");
        CSharpFile.Continue();
        CSharpFile.ElseIf("value > 100");
        CSharpFile.LineIndented("total += 100");
        CSharpFile.Else();
        CSharpFile.LineIndented("total += value");
        CSharpFile.CloseBlock();
        CSharpFile.While("total > 1000");
        CSharpFile.LineIndented("total /= 2");
        CSharpFile.Do();
        CSharpFile.OpenBlock();
        CSharpFile.Line("total++");
        CSharpFile.CloseBlock();
        CSharpFile.DoWhile("total % 2 != 0");
        CSharpFile.Switch("mode");
        CSharpFile.OpenBlock();
        CSharpFile.Case("0");
        CSharpFile.Return("total");
        CSharpFile.Case("1");
        CSharpFile.Line("total *= 2");
        CSharpFile.Break();
        CSharpFile.CaseDefault();
        CSharpFile.ThrowArgumentException("\"Unknown mode\"");
        CSharpFile.CloseBlock();
        CSharpFile.Comment("the doubled total");
        CSharpFile.Return("total");
        CSharpFile.CloseBlock();
        CSharpFile.OpenMethod("public static", "void", "Fail", "");
        CSharpFile.ThrowNotSupportedException();
        CSharpFile.CloseBlock();
        CSharpFile.OpenMethod("public static", "void", "Later", "");
        CSharpFile.ThrowNotImplementedException();
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();

        Assert.Empty(Compiler.Errors(CSharpFile.ToString()));
    }

    [Fact]
    public void DocCommentsAreValidXmlForTheirMembers()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingNamespace("System");
        CSharpFile.OpenNamespace("Written.Docs");
        CSharpFile.XmlSummary("Something with a key.");
        CSharpFile.XmlTypeParam("TKey", "The type of the key.");
        CSharpFile.OpenInterface("public", "IEntity<TKey>");
        CSharpFile.XmlSummary("The key.");
        CSharpFile.XmlValue("Never changes.");
        CSharpFile.AutoProperty("", "TKey", "Id", "get;");
        CSharpFile.CloseType();
        CSharpFile.XmlSummary("A customer.");
        CSharpFile.XmlRemarks("Kept in memory.");
        CSharpFile.XmlSeeAlso("IEntity{TKey}");
        CSharpFile.OpenClass("public sealed", "Customer", "IEntity<int>");
        CSharpFile.XmlInheritDoc();
        CSharpFile.AutoProperty("public", "int", "Id", "get; init;");
        CSharpFile.XmlInheritDoc("IEntity{TKey}.Id");
        CSharpFile.ExpressionProperty("public", "int", "Key", "Id");
        CSharpFile.XmlSummary("Finds a customer by its key.");
        CSharpFile.XmlTypeParam("T", "What the customer becomes.");
        CSharpFile.XmlParam("id", "The key; <paramref name=\"id\"/> &gt;= 0.");
        CSharpFile.XmlParam("convert", "Turns the customer into <typeparamref name=\"T\"/>.");
        CSharpFile.XmlReturns("<see langword=\"null\"/> when none has the key.");
        CSharpFile.XmlException("ArgumentOutOfRangeException", "When <paramref name=\"id\"/> is negative.");
        CSharpFile.XmlExample("<code>Find(1, Describe)</code>");
        CSharpFile.XmlLine("<para>Any other line.</para>");
        CSharpFile.OpenMethod("public", "T", "Find<T>", "int id, Func<Customer, T> convert", "where T : class");
        CSharpFile.If("id < 0");
        CSharpFile.Indent();
        CSharpFile.Throw("new ArgumentOutOfRangeException(nameof(id))");
        CSharpFile.Outdent();
        CSharpFile.Return("id == Id ? convert(this) : null");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();
        var code = CSharpFile.ToString();

        Assert.Empty(Compiler.Errors(code));
        Assert.Empty(Compiler.Warnings(code));
    }

    [Fact]
    public void TheCompilerReportsBrokenDocComments()
    {
        //the test above expects no warnings: this one proves a bad doc comment would be seen
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Broken");
        CSharpFile.XmlParam("missing", "No such parameter.");
        CSharpFile.OpenMethod("public", "void", "Run", "int id");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();

        Assert.Contains("CS1572", string.Join("\n", Compiler.Warnings(CSharpFile.ToString())));
    }

    [Fact]
    public void DirectivesCompileAndChooseTheirBranch()
    {
        CSharpFile.Start(4);
        CSharpFile.AutoGeneratedHeader();
        CSharpFile.Define("FEATURE");
        CSharpFile.Define("OTHER");
        CSharpFile.Undefine("OTHER");
        CSharpFile.UsingNamespace("System");
        CSharpFile.Nullable("enable");
        CSharpFile.OpenNamespace("Written.Directives");
        CSharpFile.OpenClass("public static", "Settings");
        CSharpFile.IfDirective("OTHER");
        CSharpFile.Line("this does not compile");
        CSharpFile.ElifDirective("FEATURE && !OTHER");
        CSharpFile.Const("public", "int", "Mode", "1");
        CSharpFile.ElseDirective();
        CSharpFile.Line("nor does this");
        CSharpFile.EndIfDirective();
        CSharpFile.PragmaWarningDisable("CS0168");
        CSharpFile.OpenMethod("public static", "string?", "Name", "");
        CSharpFile.Line("int unused");
        CSharpFile.LineDirective("hidden");
        CSharpFile.Return("null");
        CSharpFile.LineDirective("default");
        CSharpFile.CloseBlock();
        CSharpFile.PragmaWarningRestore("CS0168");
        CSharpFile.CloseType();
        CSharpFile.Nullable("restore");
        var code = CSharpFile.ToString();

        Assert.Empty(Compiler.Errors(code));
        Assert.Empty(Compiler.Warnings(code));
    }

    [Fact]
    public void WarningAndErrorDirectivesReachTheCompiler()
    {
        CSharpFile.Start(4);
        CSharpFile.WarningDirective("Check this");
        CSharpFile.OpenClass("public", "A");
        CSharpFile.CloseType();
        var warned = CSharpFile.ToString();
        CSharpFile.Start(4);
        CSharpFile.ErrorDirective("Not supported");
        var failed = CSharpFile.ToString();

        Assert.Empty(Compiler.Errors(warned));
        Assert.Contains("CS1030", string.Join("\n", Compiler.Warnings(warned)));
        Assert.Contains("CS1029", string.Join("\n", Compiler.Errors(failed)));
    }

    [Fact]
    public void NamespaceBlocksCompile()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingNamespace("System");
        CSharpFile.OpenNamespaceBlock("Written");
        CSharpFile.OpenNamespaceBlock("Models");
        CSharpFile.Record("public", "Person(string Name)");
        CSharpFile.CloseNamespaceBlock();
        CSharpFile.CloseNamespaceBlock();
        CSharpFile.OpenNamespaceBlock("Written.Services");
        CSharpFile.UsingNamespace("Written.Models");
        CSharpFile.OpenClass("public static", "People");
        CSharpFile.ExpressionMethod("public static", "Person", "Make", "string name", "new Person(name)");
        CSharpFile.CloseType();
        CSharpFile.CloseNamespaceBlock();

        Assert.Empty(Compiler.Errors(CSharpFile.ToString()));
    }

    [Fact]
    public void CloseTypeWithNamespaceCompilesWithBothKindsOfNamespace()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenNamespaceBlock("Written.First");
        CSharpFile.OpenClass("public", "A");
        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.OpenNamespaceBlock("Written.Second");
        CSharpFile.OpenClass("public", "B");
        CSharpFile.CloseTypeWithNamespace();
        var blocks = CSharpFile.ToString();

        CSharpFile.Start(4);
        CSharpFile.OpenNamespace("Written");
        CSharpFile.OpenClass("public", "A");
        CSharpFile.CloseTypeWithNamespace();
        var fileScoped = CSharpFile.ToString();

        Assert.Empty(Compiler.Errors(blocks));
        Assert.Empty(Compiler.Errors(fileScoped));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void EveryStatementCompiles(int indentSize)
    {
        WriteEveryStatement(indentSize);

        Assert.Empty(Compiler.Errors(CSharpFile.ToString()));
    }

    /// <summary>
    /// One file with every statement the writer has, inside methods the way real code uses them.
    /// </summary>
    private static void WriteEveryStatement(int indentSize)
    {
        CSharpFile.Start(indentSize);
        CSharpFile.UsingNamespace("System");
        CSharpFile.UsingNamespace("System.Collections.Generic");
        CSharpFile.UsingNamespace("System.IO");
        CSharpFile.UsingNamespace("System.Threading.Tasks");
        CSharpFile.OpenNamespace("Written.Statements");
        CSharpFile.OpenClass("public static", "Everything");

        //loops and checked blocks
        CSharpFile.OpenMethod("public static", "int", "Loops", "int[] values, Dictionary<string, int> map");
        CSharpFile.Line("var total = 0");
        CSharpFile.For("var i = 0", "i < values.Length", "i++");
        CSharpFile.LineIndented("total += values[i]");
        CSharpFile.For("", "", "");
        CSharpFile.OpenBlock();
        CSharpFile.Line("total++");
        CSharpFile.If("total > 10");
        CSharpFile.Indent();
        CSharpFile.Break();
        CSharpFile.Outdent();
        CSharpFile.CloseBlock();
        CSharpFile.For("int i = 0, j = values.Length - 1", "i < j", "i++, j--");
        CSharpFile.LineIndented("total += values[i] - values[j]");
        CSharpFile.ForEach("int", "value", "values");
        CSharpFile.LineIndented("total -= value");
        CSharpFile.ForEach("(key, count)", "map");
        CSharpFile.LineIndented("total += key.Length * count");
        CSharpFile.Checked();
        CSharpFile.OpenBlock();
        CSharpFile.Line("total *= 2");
        CSharpFile.CloseBlock();
        CSharpFile.Unchecked();
        CSharpFile.OpenBlock();
        CSharpFile.Line("total += int.MaxValue");
        CSharpFile.CloseBlock();
        CSharpFile.Return("total");
        CSharpFile.CloseBlock();

        //exception handling and resources
        CSharpFile.OpenMethod("public static", "string", "Read", "string text");
        CSharpFile.Try();
        CSharpFile.OpenBlock();
        CSharpFile.Using("var reader = new StringReader(text)");
        CSharpFile.OpenBlock();
        CSharpFile.Return("reader.ReadLine()");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.Catch("ArgumentNullException");
        CSharpFile.OpenBlock();
        CSharpFile.Rethrow();
        CSharpFile.CloseBlock();
        CSharpFile.Catch("IOException ex", "ex.Message.Length > 0");
        CSharpFile.OpenBlock();
        CSharpFile.Throw("new InvalidOperationException(\"read\", ex)");
        CSharpFile.CloseBlock();
        CSharpFile.Catch("Exception ex");
        CSharpFile.OpenBlock();
        CSharpFile.Return("ex.Message");
        CSharpFile.CloseBlock();
        CSharpFile.Catch();
        CSharpFile.OpenBlock();
        CSharpFile.Return("null");
        CSharpFile.CloseBlock();
        CSharpFile.Finally();
        CSharpFile.OpenBlock();
        CSharpFile.Line("text = null");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        //lock and a using declaration
        CSharpFile.OpenMethod("public static", "void", "Locked", "object gate, List<int> items");
        CSharpFile.Lock("gate");
        CSharpFile.OpenBlock();
        CSharpFile.UsingDeclaration("var stream = new MemoryStream()");
        CSharpFile.Line("items.Add((int)stream.Length)");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        //an iterator
        CSharpFile.OpenMethod("public static", "IEnumerable<int>", "Evens", "int max");
        CSharpFile.For("var i = 0", "i < max", "i++");
        CSharpFile.OpenBlock();
        CSharpFile.If("i > 100");
        CSharpFile.Indent();
        CSharpFile.YieldBreak();
        CSharpFile.Outdent();
        CSharpFile.If("i % 2 == 0");
        CSharpFile.Indent();
        CSharpFile.YieldReturn("i");
        CSharpFile.Outdent();
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        //case guards and jumps
        CSharpFile.OpenMethod("public static", "string", "Jumps", "object value, int mode");
        CSharpFile.Switch("value");
        CSharpFile.OpenBlock();
        CSharpFile.CaseWhen("int number", "number > 0");
        CSharpFile.Indent();
        CSharpFile.Return("\"positive\"");
        CSharpFile.Outdent();
        CSharpFile.Case("int");
        CSharpFile.Indent();
        CSharpFile.GotoDefault();
        CSharpFile.Outdent();
        CSharpFile.CaseDefault();
        CSharpFile.Indent();
        CSharpFile.Break();
        CSharpFile.Outdent();
        CSharpFile.CloseBlock();
        CSharpFile.Switch("mode");
        CSharpFile.OpenBlock();
        CSharpFile.Case("1");
        CSharpFile.Indent();
        CSharpFile.GotoCase("2");
        CSharpFile.Outdent();
        CSharpFile.Case("2");
        CSharpFile.Indent();
        CSharpFile.Goto("done");
        CSharpFile.Outdent();
        CSharpFile.CaseDefault();
        CSharpFile.Indent();
        CSharpFile.Break();
        CSharpFile.Outdent();
        CSharpFile.CloseBlock();
        CSharpFile.Return("\"other\"");
        CSharpFile.Label("done");
        CSharpFile.Return("\"done\"");
        CSharpFile.CloseBlock();

        //unsafe code
        CSharpFile.OpenMethod("public static unsafe", "int", "Pinned", "byte[] buffer");
        CSharpFile.Fixed("byte* first = buffer");
        CSharpFile.OpenBlock();
        CSharpFile.Return("*first");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.OpenMethod("public static", "int", "Pointer", "int value");
        CSharpFile.Unsafe();
        CSharpFile.OpenBlock();
        CSharpFile.Line("int* pointer = &value");
        CSharpFile.Return("*pointer");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        //async streams and async disposal
        CSharpFile.OpenMethod("public static async", "IAsyncEnumerable<int>", "Numbers", "");
        CSharpFile.Line("await Task.Yield()");
        CSharpFile.YieldReturn("1");
        CSharpFile.CloseBlock();
        CSharpFile.OpenMethod("public static async", "Task<int>", "SumAsync", "");
        CSharpFile.Line("var total = 0");
        CSharpFile.AwaitUsingDeclaration("var stream = new MemoryStream()");
        CSharpFile.AwaitUsing("var other = new MemoryStream()");
        CSharpFile.OpenBlock();
        CSharpFile.Line("total += (int)other.Length + (int)stream.Length");
        CSharpFile.CloseBlock();
        CSharpFile.AwaitForEach("number", "Numbers()");
        CSharpFile.LineIndented("total += number");
        CSharpFile.AwaitForEach("int", "number", "Numbers()");
        CSharpFile.LineIndented("total -= number");
        CSharpFile.Return("total");
        CSharpFile.CloseBlock();

        CSharpFile.CloseType();
    }

    /// <summary>
    /// One file with every declaration the writer has, used by one another the way real code does.
    /// </summary>
    private static void WriteEveryDeclaration(int indentSize)
    {
        CSharpFile.Start(indentSize);
        CSharpFile.UsingNamespace("System");
        CSharpFile.UsingNamespace("System.Collections.Generic");
        CSharpFile.UsingStatic("System.Math");
        CSharpFile.UsingAlias("Text", "System.String");
        CSharpFile.EmptyLine();
        CSharpFile.Attribute("assembly: System.Reflection.AssemblyTitle(\"Written\")");
        CSharpFile.EmptyLine();
        CSharpFile.OpenNamespace("Written.Models");
        CSharpFile.EmptyLine();

        //enums
        CSharpFile.Attribute("Flags");
        CSharpFile.OpenEnum("public", "Access", "byte");
        CSharpFile.EnumMember("None", "0");
        CSharpFile.EnumMember("Read", "1");
        CSharpFile.EnumMember("Write", "2");
        CSharpFile.EnumMember("All", "Read | Write");
        CSharpFile.CloseType();
        CSharpFile.OpenEnum("internal", "Color");
        CSharpFile.EnumMember("Red");
        CSharpFile.EnumMember("Green");
        CSharpFile.CloseType();

        //interfaces: a property, a signature, a default method, a static abstract member
        CSharpFile.XmlSummary("Something with a key.");
        CSharpFile.OpenInterface("public", "IEntity<TKey>", null, "where TKey : IEquatable<TKey>");
        CSharpFile.AutoProperty("", "TKey", "Id", "get;");
        CSharpFile.MethodSignature("", "bool", "Matches", "TKey other");
        CSharpFile.OpenMethod("", "string", "Describe", "");
        CSharpFile.Return("\"entity \" + Id");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();
        CSharpFile.OpenInterface("public", "IFactory<T>", null, "where T : IFactory<T>");
        CSharpFile.MethodSignature("static abstract", "T", "Create", "");
        CSharpFile.CloseType();

        //an abstract generic class: constants, fields, a protected constructor, an abstract method
        CSharpFile.Attribute("Serializable");
        CSharpFile.OpenClass("public abstract", "Entity<TKey>", "IEntity<TKey>", "where TKey : IEquatable<TKey>");
        CSharpFile.Const("public", "int", "MaxNameLength", "100");
        CSharpFile.Const("protected", "string", "Prefix", "\"E-\"");
        CSharpFile.Field("private static readonly", "object", "_gate", "new object()");
        CSharpFile.Field("protected", "TKey", "_id");
        CSharpFile.OpenConstructor("protected", "Entity", "TKey id");
        CSharpFile.Line("_id = id");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionProperty("public", "TKey", "Id", "_id");
        CSharpFile.ExpressionProperty("protected static", "object", "Gate", "_gate");
        CSharpFile.MethodSignature("public abstract", "string", "Name", "");
        CSharpFile.ExpressionMethod("public", "bool", "Matches", "TKey other", "_id.Equals(other)");
        CSharpFile.OpenMethod("public virtual", "string", "Describe", "");
        CSharpFile.Return("Prefix + Name()");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();

        //a sealed class: every kind of constructor, property, accessor and method
        CSharpFile.OpenClass("public sealed", "Customer", "Entity<int>, IFactory<Customer>");
        CSharpFile.Field("private", "string", "_name", "string.Empty");
        CSharpFile.Field("private", "int", "_age");
        CSharpFile.Field("private static", "int", "_created");
        CSharpFile.OpenConstructor("static", "Customer", null);
        CSharpFile.Line("_created = 0");
        CSharpFile.CloseBlock();
        CSharpFile.OpenConstructor("public", "Customer", "int id, string name", "base(id)");
        CSharpFile.Line("_name = name");
        CSharpFile.CloseBlock();
        CSharpFile.OpenConstructor("public", "Customer", "", "this(0, \"new\")");
        CSharpFile.CloseBlock();
        CSharpFile.OpenFinalizer("Customer");
        CSharpFile.Line("_created--");
        CSharpFile.CloseBlock();
        CSharpFile.AutoProperty("public", "string", "Email");
        CSharpFile.AutoProperty("public", "DateTime", "Joined", "get; init;");
        CSharpFile.AutoProperty("public", "List<string>", "Tags", "get; private set;", "new List<string>()");
        CSharpFile.OpenProperty("public", "int", "Age");
        CSharpFile.Get("_age");
        CSharpFile.Set("_age = Max(0, value)");
        CSharpFile.CloseBlock();
        CSharpFile.OpenProperty("public", "Text", "Display");
        CSharpFile.OpenGet();
        CSharpFile.Return("_name + \" (\" + _age + \")\"");
        CSharpFile.CloseBlock();
        CSharpFile.OpenAccessor("private set");
        CSharpFile.Line("_name = value");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.OpenProperty("public", "string", "Nick");
        CSharpFile.Get("_name");
        CSharpFile.Init("_name = value");
        CSharpFile.CloseBlock();
        CSharpFile.OpenProperty("public", "int", "Level");
        CSharpFile.OpenGet();
        CSharpFile.Return("_age / 10");
        CSharpFile.CloseBlock();
        CSharpFile.OpenInit();
        CSharpFile.Line("_age = value * 10");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.OpenProperty("public", "string", "Code");
        CSharpFile.Get("_name");
        CSharpFile.Accessor("protected set", "_name = value");
        CSharpFile.CloseBlock();
        CSharpFile.OpenProperty("public", "string", "Alias");
        CSharpFile.OpenGet();
        CSharpFile.Return("_name");
        CSharpFile.CloseBlock();
        CSharpFile.OpenSet();
        CSharpFile.Line("_name = value");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionMethod("public static", "Customer", "Create", "", "new Customer()");
        CSharpFile.ExpressionMethod("public override", "string", "Name", "", "_name");
        CSharpFile.OpenMethod("public", "T", "Convert<T>", "Func<Customer, T> converter", "where T : class");
        CSharpFile.Return("converter(this)");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionMethod("public", "T", "Make<T>", "", "new T()", "where T : new()");
        CSharpFile.OpenMethod("public", "void", "Update", "ref int count, out string name, in int step, int extra = 1, params string[] tags");
        CSharpFile.Line("count += step + extra + tags.Length");
        CSharpFile.Line("name = _name");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();

        //a struct: get-only properties, a constructor with a body and one with an expression body
        CSharpFile.OpenStruct("public readonly", "Point", "IEquatable<Point>");
        CSharpFile.AutoProperty("public", "int", "X", "get;");
        CSharpFile.AutoProperty("public", "int", "Y", "get;");
        CSharpFile.OpenConstructor("public", "Point", "int x, int y");
        CSharpFile.Line("X = x");
        CSharpFile.Line("Y = y");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionConstructor("public", "Point", "int both", "(X, Y) = (both, both)");
        CSharpFile.ExpressionMethod("public", "bool", "Equals", "Point other", "X == other.X && Y == other.Y");
        CSharpFile.CloseType();
        CSharpFile.OpenStruct("public", "Pair<T>", null, "where T : unmanaged");
        CSharpFile.Field("public", "T", "First");
        CSharpFile.Field("public", "T", "Second");
        CSharpFile.CloseType();

        //records: positional without a body, with a body, generic with constraints, record structs
        CSharpFile.Record("public", "Address(string Street, string City)");
        CSharpFile.Record("public", "Box<T>(T Value)", null, "where T : class");
        CSharpFile.Record("public", "Labeled<T>(T Value, string Label)", "Box<T>(Value)", "where T : class");
        CSharpFile.RecordStruct("public readonly", "Size(int Width, int Height)");
        CSharpFile.OpenRecord("public", "Order(int Id, decimal Total)", "IEntity<int>");
        CSharpFile.ExpressionMethod("public", "bool", "Matches", "int other", "Id == other");
        CSharpFile.CloseType();
        CSharpFile.OpenRecord("public", "Shipment<T>(T Item)", null, "where T : notnull");
        CSharpFile.AutoProperty("public", "int", "Count", "get; init;", "1");
        CSharpFile.CloseType();
        CSharpFile.OpenRecordStruct("public", "Span(int Start, int End)");
        CSharpFile.ExpressionProperty("public", "int", "Length", "End - Start");
        CSharpFile.CloseType();

        //a primary constructor and a nested generic type
        CSharpFile.OpenClass("public", "Report(string title)");
        CSharpFile.ExpressionMethod("public", "string", "Title", "", "title");
        CSharpFile.OpenClass("private sealed", "Page<T>", null, "where T : struct");
        CSharpFile.Field("public", "T", "Value");
        CSharpFile.CloseType();
        CSharpFile.CloseType();
        CSharpFile.OpenClass("public sealed", "Summary(string title, int pages)", "Report(title)");
        CSharpFile.ExpressionProperty("public", "int", "Pages", "pages");
        CSharpFile.CloseType();

        //delegates
        CSharpFile.Delegate("public", "void", "Changed", "object sender, string name");
        CSharpFile.Delegate("public", "T", "Factory<T>", "int id", "where T : class");
        CSharpFile.Delegate("internal", "int", "Counter", null);

        //operators: a static abstract one in an interface, implemented with every form, plus conversions
        CSharpFile.OpenInterface("public", "IAddable<T>", null, "where T : IAddable<T>");
        CSharpFile.OperatorSignature("static abstract", "T", "+", "T left, T right");
        CSharpFile.IndexerSignature("", "string", "int index", "get;");
        CSharpFile.CloseType();
        CSharpFile.OpenStruct("public readonly", "Money", "IAddable<Money>");
        CSharpFile.Field("private readonly", "decimal", "_amount");
        CSharpFile.ExpressionConstructor("public", "Money", "decimal amount", "_amount = amount");
        CSharpFile.ExpressionOperator("public static", "Money", "+", "Money left, Money right", "new Money(left._amount + right._amount)");
        CSharpFile.OpenOperator("public static", "Money", "-", "Money value");
        CSharpFile.Return("new Money(-value._amount)");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionOperator("public static", "bool", "true", "Money value", "value._amount != 0");
        CSharpFile.ExpressionOperator("public static", "bool", "false", "Money value", "value._amount == 0");
        CSharpFile.ExpressionConversion("public static", "implicit", "decimal", "Money value", "value._amount");
        CSharpFile.OpenConversion("public static", "explicit", "Money", "decimal amount");
        CSharpFile.Return("new Money(amount)");
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionIndexer("public", "string", "int index", "_amount.ToString().Substring(index, 1)");
        CSharpFile.CloseType();

        //indexers and events
        CSharpFile.OpenClass("public sealed", "Catalog");
        CSharpFile.Field("private readonly", "List<string>", "_items", "new List<string>()");
        CSharpFile.Field("private", "EventHandler", "_updated");
        CSharpFile.Field("private", "EventHandler", "_reset");
        CSharpFile.Event("public", "EventHandler", "Cleared");
        CSharpFile.OpenEvent("public", "EventHandler", "Updated");
        CSharpFile.Add("_updated += value");
        CSharpFile.OpenRemove();
        CSharpFile.Line("_updated -= value");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.OpenEvent("public", "EventHandler", "Reset");
        CSharpFile.OpenAdd();
        CSharpFile.Line("_reset += value");
        CSharpFile.CloseBlock();
        CSharpFile.Remove("_reset -= value");
        CSharpFile.CloseBlock();
        CSharpFile.OpenIndexer("public", "string", "int index");
        CSharpFile.Get("_items[index]");
        CSharpFile.OpenSet();
        CSharpFile.Line("_items[index] = value");
        CSharpFile.Line("_updated?.Invoke(this, EventArgs.Empty)");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.ExpressionIndexer("public", "int", "string name", "_items.IndexOf(name)");
        CSharpFile.OpenMethod("public", "void", "Clear", "");
        CSharpFile.Line("_items.Clear()");
        CSharpFile.Line("Cleared?.Invoke(this, EventArgs.Empty)");
        CSharpFile.Line("_reset?.Invoke(this, EventArgs.Empty)");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();
        CSharpFile.OpenClass("public abstract", "Shape");
        CSharpFile.IndexerSignature("public abstract", "double", "int side", "get;");
        CSharpFile.CloseType();
        CSharpFile.OpenClass("public sealed", "Square", "Shape");
        CSharpFile.ExpressionIndexer("public override", "double", "int side", "1.0");
        CSharpFile.CloseType();

        //a partial method declared and implemented
        CSharpFile.OpenClass("public partial", "Hooks");
        CSharpFile.MethodSignature("partial", "void", "OnSaved", "");
        CSharpFile.CloseType();
        CSharpFile.OpenClass("public partial", "Hooks");
        CSharpFile.OpenMethod("partial", "void", "OnSaved", "");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();
    }
}

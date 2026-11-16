#region Usings
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// Member declarations: fields, constants, properties and their accessors, methods with their parameters, and
/// constructors. Each test starts a new file and checks the exact text.
/// </summary>
public sealed class MemberTests
{
    #region Fields and Constants
    [Fact]
    public void FieldIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.Field("private static readonly", "object", "_gate");

        Assert.Equal(Code.Lines("private static readonly object _gate;"), CSharpFile.ToString());
    }

    [Fact]
    public void FieldWritesItsInitializer()
    {
        CSharpFile.Start(4);
        CSharpFile.Field("private", "List<string>", "_names", "new List<string>()");

        Assert.Equal(Code.Lines("private List<string> _names = new List<string>();"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FieldWithoutModifiersStartsWithTheType(string modifiers)
    {
        CSharpFile.Start(4);
        CSharpFile.Field(modifiers, "int", "_count", "0");

        Assert.Equal(Code.Lines("int _count = 0;"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("int", "MaxLength", "100", "public const int MaxLength = 100;")]
    [InlineData("string", "Prefix", "\"E-\"", "public const string Prefix = \"E-\";")]
    [InlineData("decimal", "Rate", "1.5m", "public const decimal Rate = 1.5m;")]
    [InlineData("bool", "Enabled", "true", "public const bool Enabled = true;")]
    [InlineData("char", "Separator", "','", "public const char Separator = ',';")]
    public void ConstWritesItsTypeAndValue(string type, string name, string value, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.Const("public", type, name, value);

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }
    #endregion

    #region Auto and Expression Properties
    [Fact]
    public void AutoPropertyHasAGetterAndASetter()
    {
        CSharpFile.Start(4);
        CSharpFile.AutoProperty("public", "string", "Name");

        Assert.Equal(Code.Lines("public string Name { get; set; }"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("get;", "public int Id { get; }")]
    [InlineData("get; init;", "public int Id { get; init; }")]
    [InlineData("get; private set;", "public int Id { get; private set; }")]
    public void AutoPropertyWritesTheAccessorsGiven(string accessors, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.AutoProperty("public", "int", "Id", accessors);

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }

    [Fact]
    public void AutoPropertyWritesItsInitializer()
    {
        CSharpFile.Start(4);
        CSharpFile.AutoProperty("public", "List<string>", "Tags", "get; set;", "new List<string>()");

        Assert.Equal(Code.Lines("public List<string> Tags { get; set; } = new List<string>();"), CSharpFile.ToString());
    }

    [Fact]
    public void InterfacePropertyHasNoModifiers()
    {
        CSharpFile.Start(4);
        CSharpFile.AutoProperty("", "int", "Count", "get;");

        Assert.Equal(Code.Lines("int Count { get; }"), CSharpFile.ToString());
    }

    [Fact]
    public void ExpressionPropertyIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.ExpressionProperty("public", "int", "Length", "End - Start");

        Assert.Equal(Code.Lines("public int Length => End - Start;"), CSharpFile.ToString());
    }
    #endregion

    #region Properties with Accessors
    [Fact]
    public void PropertyWithExpressionAccessors()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenProperty("public", "int", "Count");
        CSharpFile.Get("_count");
        CSharpFile.Set("_count = value");
        CSharpFile.CloseBlock();

        var expected = Code.Lines("public int Count", "{", "    get => _count;", "    set => _count = value;", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void PropertyWithAccessorBodies()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenProperty("public", "string", "Name");
        CSharpFile.OpenGet();
        CSharpFile.Return("_name");
        CSharpFile.CloseBlock();
        CSharpFile.OpenSet();
        CSharpFile.Line("_name = value");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        var expected = Code.Lines(
            "public string Name",
            "{",
            "    get",
            "    {",
            "        return _name;",
            "    }",
            "    set",
            "    {",
            "        _name = value;",
            "    }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void PropertyWithInitAccessors()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenProperty("public", "int", "Level");
        CSharpFile.Get("_level");
        CSharpFile.Init("_level = value");
        CSharpFile.CloseBlock();
        CSharpFile.OpenProperty("public", "int", "Rank");
        CSharpFile.Get("_rank");
        CSharpFile.OpenInit();
        CSharpFile.Line("_rank = value");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        var expected = Code.Lines(
            "public int Level",
            "{",
            "    get => _level;",
            "    init => _level = value;",
            "}",
            "public int Rank",
            "{",
            "    get => _rank;",
            "    init",
            "    {",
            "        _rank = value;",
            "    }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void PropertyWithAccessorModifiers()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenProperty("public", "string", "Code");
        CSharpFile.Get("_code");
        CSharpFile.Accessor("protected set", "_code = value");
        CSharpFile.CloseBlock();
        CSharpFile.OpenProperty("public", "string", "Key");
        CSharpFile.Get("_key");
        CSharpFile.OpenAccessor("private set");
        CSharpFile.Line("_key = value");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        var expected = Code.Lines(
            "public string Code",
            "{",
            "    get => _code;",
            "    protected set => _code = value;",
            "}",
            "public string Key",
            "{",
            "    get => _key;",
            "    private set",
            "    {",
            "        _key = value;",
            "    }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion

    #region Methods and Parameters
    [Fact]
    public void MethodOpensWithItsParameters()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenMethod("public", "void", "Save", "int id, string name");
        CSharpFile.Line("_id = id");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("public void Save(int id, string name)", "{", "    _id = id;", "}"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MethodWithoutParametersHasEmptyParentheses(string parameters)
    {
        CSharpFile.Start(4);
        CSharpFile.OpenMethod("", "void", "Run", parameters);
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("void Run()", "{", "}"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("ref int count")]
    [InlineData("out string name")]
    [InlineData("in Point point")]
    [InlineData("ref readonly Point point")]
    [InlineData("params int[] values")]
    [InlineData("int step = 1, string label = null")]
    [InlineData("[NotNull] string text")]
    [InlineData("this string text")]
    public void MethodWritesItsParametersAsGiven(string parameters)
    {
        CSharpFile.Start(4);
        CSharpFile.MethodSignature("", "void", "Run", parameters);

        Assert.Equal(Code.Lines("void Run(" + parameters + ");"), CSharpFile.ToString());
    }

    [Fact]
    public void MethodWritesItsConstraintsOnTheirOwnLine()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenMethod("public", "T", "Find<T>", "int id", "where T : class");
        CSharpFile.Return("null");
        CSharpFile.CloseBlock();

        var expected = Code.Lines("public T Find<T>(int id)", "    where T : class", "{", "    return null;", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ExpressionMethodIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.ExpressionMethod("public static", "int", "Add", "int a, int b", "a + b");

        Assert.Equal(Code.Lines("public static int Add(int a, int b) => a + b;"), CSharpFile.ToString());
    }

    [Fact]
    public void ExpressionMethodWritesItsConstraintsBeforeTheBody()
    {
        CSharpFile.Start(4);
        CSharpFile.ExpressionMethod("public", "T", "Make<T>", "", "new T()", "where T : new()");

        Assert.Equal(Code.Lines("public T Make<T>()", "    where T : new() => new T();"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("", "void Save(int id);")]
    [InlineData("public abstract", "public abstract void Save(int id);")]
    [InlineData("partial", "partial void Save(int id);")]
    public void MethodSignatureEndsWithASemicolon(string modifiers, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.MethodSignature(modifiers, "void", "Save", "int id");

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }

    [Fact]
    public void MethodSignatureEndsItsConstraintsWithTheSemicolon()
    {
        CSharpFile.Start(4);
        CSharpFile.MethodSignature("", "T", "Make<T>", "", "where T : new()");

        Assert.Equal(Code.Lines("T Make<T>()", "    where T : new();"), CSharpFile.ToString());
    }
    #endregion

    #region Constructors
    [Fact]
    public void ConstructorOpensWithItsParameters()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenConstructor("public", "Person", "string name");
        CSharpFile.Line("_name = name");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("public Person(string name)", "{", "    _name = name;", "}"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("base(name)")]
    [InlineData("this(name, 0)")]
    public void ConstructorWritesItsInitializerOnItsOwnLine(string initializer)
    {
        CSharpFile.Start(4);
        CSharpFile.OpenConstructor("public", "Customer", "string name", initializer);
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("public Customer(string name)", "    : " + initializer, "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void StaticConstructorHasNoParameters()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenConstructor("static", "Cache", null);
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("static Cache()", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void ExpressionConstructorIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.ExpressionConstructor("public", "Person", "string name", "Name = name");

        Assert.Equal(Code.Lines("public Person(string name) => Name = name;"), CSharpFile.ToString());
    }

    [Fact]
    public void FinalizerOpens()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenFinalizer("Person");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("~Person()", "{", "}"), CSharpFile.ToString());
    }
    #endregion

    #region Indexers
    [Fact]
    public void IndexerWithAccessors()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenIndexer("public", "string", "int index");
        CSharpFile.Get("_items[index]");
        CSharpFile.OpenSet();
        CSharpFile.Line("_items[index] = value");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        var expected = Code.Lines(
            "public string this[int index]",
            "{",
            "    get => _items[index];",
            "    set",
            "    {",
            "        _items[index] = value;",
            "    }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ExpressionIndexerIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.ExpressionIndexer("public", "int", "string name, int start = 0", "_items.IndexOf(name, start)");

        Assert.Equal(Code.Lines("public int this[string name, int start = 0] => _items.IndexOf(name, start);"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("", "get;", "string this[int index] { get; }")]
    [InlineData("public abstract", "get; set;", "public abstract string this[int index] { get; set; }")]
    public void IndexerSignatureHasNoBodies(string modifiers, string accessors, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.IndexerSignature(modifiers, "string", "int index", accessors);

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }
    #endregion

    #region Events
    [Fact]
    public void FieldLikeEventIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.Event("public", "EventHandler<string>", "Changed");

        Assert.Equal(Code.Lines("public event EventHandler<string> Changed;"), CSharpFile.ToString());
    }

    [Fact]
    public void EventWithExpressionAccessors()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenEvent("public", "EventHandler", "Changed");
        CSharpFile.Add("_changed += value");
        CSharpFile.Remove("_changed -= value");
        CSharpFile.CloseBlock();

        var expected = Code.Lines(
            "public event EventHandler Changed",
            "{",
            "    add => _changed += value;",
            "    remove => _changed -= value;",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void EventWithAccessorBodies()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenEvent("", "EventHandler", "Reset");
        CSharpFile.OpenAdd();
        CSharpFile.Line("_reset += value");
        CSharpFile.CloseBlock();
        CSharpFile.OpenRemove();
        CSharpFile.Line("_reset -= value");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        var expected = Code.Lines(
            "event EventHandler Reset",
            "{",
            "    add",
            "    {",
            "        _reset += value;",
            "    }",
            "    remove",
            "    {",
            "        _reset -= value;",
            "    }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion

    #region Operators and Conversions
    [Fact]
    public void OperatorWithABody()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenOperator("public static", "Money", "-", "Money value");
        CSharpFile.Return("new Money(-value.Amount)");
        CSharpFile.CloseBlock();

        var expected = Code.Lines("public static Money operator -(Money value)", "{", "    return new Money(-value.Amount);", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Theory]
    [InlineData("+", "Money left, Money right", "public static Money operator +(Money left, Money right) => Add(left, right);")]
    [InlineData("==", "Money left, Money right", "public static Money operator ==(Money left, Money right) => Add(left, right);")]
    [InlineData("true", "Money value", "public static Money operator true(Money value) => Add(left, right);")]
    public void ExpressionOperatorIsOneStatement(string op, string parameters, string expected)
    {
        CSharpFile.Start(4);
        CSharpFile.ExpressionOperator("public static", "Money", op, parameters, "Add(left, right)");

        Assert.Equal(Code.Lines(expected), CSharpFile.ToString());
    }

    [Fact]
    public void OperatorSignatureEndsWithASemicolon()
    {
        CSharpFile.Start(4);
        CSharpFile.OperatorSignature("static abstract", "T", "+", "T left, T right");

        Assert.Equal(Code.Lines("static abstract T operator +(T left, T right);"), CSharpFile.ToString());
    }

    [Fact]
    public void CompoundAssignmentOperatorIsAnInstanceOperator()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenOperator("public", "void", "+=", "int amount");
        CSharpFile.Line("Amount += amount");
        CSharpFile.CloseBlock();

        Assert.Equal(Code.Lines("public void operator +=(int amount)", "{", "    Amount += amount;", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void ConversionWithABody()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenConversion("public static", "explicit", "Money", "decimal amount");
        CSharpFile.Return("new Money(amount)");
        CSharpFile.CloseBlock();

        var expected = Code.Lines("public static explicit operator Money(decimal amount)", "{", "    return new Money(amount);", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ExpressionConversionIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.ExpressionConversion("public static", "implicit", "decimal", "Money value", "value.Amount");

        Assert.Equal(Code.Lines("public static implicit operator decimal(Money value) => value.Amount;"), CSharpFile.ToString());
    }
    #endregion

    #region Members inside a Type
    [Fact]
    public void MembersInsideAClassAreIndented()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public sealed", "Customer", "Person");
        CSharpFile.Const("private", "int", "Max", "10");
        CSharpFile.OpenConstructor("public", "Customer", "string name", "base(name)");
        CSharpFile.CloseBlock();
        CSharpFile.OpenMethod("public", "T", "Find<T>", "", "where T : class");
        CSharpFile.Return("null");
        CSharpFile.CloseBlock();
        CSharpFile.CloseType();

        var expected = Code.Lines(
            "public sealed class Customer : Person",
            "{",
            "    private const int Max = 10;",
            "    public Customer(string name)",
            "        : base(name)",
            "    {",
            "    }",
            "    public T Find<T>()",
            "        where T : class",
            "    {",
            "        return null;",
            "    }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion
}

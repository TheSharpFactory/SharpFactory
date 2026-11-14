#region Usings
using SF.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
#endregion

namespace SF.Tests;

/// <summary>
/// The C# writer (SF.CodeGeneration.CSharpFile) on its own: indentation (a level, written as spaces straight into the
/// file), lines and semicolons, blocks and control flow, declarations, doc comments and the file header. Each test
/// starts a new file and checks the exact text.
/// </summary>
public sealed class CSharpFileTests
{
    #region Indentation
    [Fact]
    public void BlocksIndentOneLevelEach()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Line("a");
        CSharpFile.OpenBlock();
        CSharpFile.Line("b");
        CSharpFile.OpenBlock();
        CSharpFile.Line("c");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        Assert.Equal("{\n    a;\n    {\n        b;\n        {\n            c;\n        }\n    }\n}\n", Text());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    public void EveryIndentSizeIsHonored(int size)
    {
        CSharpFile.Start(size);
        CSharpFile.OpenBlock();
        CSharpFile.Line("a");
        CSharpFile.OpenBlock();
        CSharpFile.Line("b");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        var one = new string(' ', size);
        var two = new string(' ', size * 2);
        Assert.Equal("{\n" + one + "a;\n" + one + "{\n" + two + "b;\n" + one + "}\n}\n", Text());
    }

    [Fact]
    public void DeepNestingWritesAllItsSpaces()
    {
        CSharpFile.Start(4);
        for(var i = 0; i < 40; i++)
            CSharpFile.OpenBlock();

        CSharpFile.Line("x");
        for(var i = 0; i < 40; i++)
            CSharpFile.CloseBlock();

        var lines = Text().Split('\n');
        Assert.Equal(new string(' ', 160) + "x;", lines[40]);
        Assert.Equal(new string(' ', 156) + "}", lines[41]);
        Assert.Equal("}", lines[80]);
    }

    [Fact]
    public void ClosingMoreThanWasOpenedStopsAtTheLeftMargin()
    {
        CSharpFile.Start(4);
        CSharpFile.CloseBlock();
        CSharpFile.Line("a");
        CSharpFile.Outdent();
        CSharpFile.Outdent();
        CSharpFile.Indent();
        CSharpFile.Line("b");

        Assert.Equal("}\na;\n    b;\n", Text());
    }

    [Fact]
    public void IndentAndOutdentMoveTheNextLine()
    {
        CSharpFile.Start(2);
        CSharpFile.Indent();
        CSharpFile.Indent();
        CSharpFile.Line("a");
        CSharpFile.Outdent();
        CSharpFile.Line("b");

        Assert.Equal("    a;\n  b;\n", Text());
    }

    [Fact]
    public void StartBeginsANewFileAtTheLeftMargin()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.OpenBlock();
        CSharpFile.Line("left over");

        CSharpFile.Start(4);
        CSharpFile.Line("x");

        Assert.Equal("x;\n", Text());
    }

    [Fact]
    public void StartWithoutASizeKeepsTheLastSize()
    {
        CSharpFile.Start(2);
        CSharpFile.Start();
        CSharpFile.OpenBlock();
        CSharpFile.Line("x");
        CSharpFile.CloseBlock();

        Assert.Equal("{\n  x;\n}\n", Text());
    }

    [Fact]
    public void TheSameCallsWriteTheSameFileTwice()
    {
        var first = WriteSample();
        var second = WriteSample();

        Assert.Equal(first, second);
        Assert.Contains("    public partial class Sample : Base<A, B>, ISample", first, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendIndentedWritesTheIndentWithoutEndingTheLine()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.AppendIndented("var x = ");
        CSharpFile.Append("1");
        CSharpFile.CloseLine();

        Assert.Equal("{\n    var x = 1;\n", Text());
    }
    #endregion

    #region Lines
    [Fact]
    public void LineAddsASemicolonUnlessItEndsInOneOrAComma()
    {
        CSharpFile.Start(4);
        CSharpFile.Line("a");
        CSharpFile.Line("b;");
        CSharpFile.Line("c,");
        CSharpFile.Line("");
        CSharpFile.LineNoEnding("d");
        CSharpFile.CloseLine();
        CSharpFile.CloseLine("e");

        Assert.Equal("a;\nb;\nc,\n;\nd\n;\ne;\n", Text());
    }

    [Fact]
    public void ALineWithACommentHasASpaceBeforeIt()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Line("var x = 1", "one");

        Assert.Equal("{\n    var x = 1; // one\n", Text());
    }

    [Fact]
    public void IndentedLinesGoOneLevelDeeperThanTheirBlock()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.If("a");
        CSharpFile.LineIndented("b()");
        CSharpFile.If("c");
        CSharpFile.ReturnIndented("d");
        CSharpFile.If("e");
        CSharpFile.ReturnIndented();
        CSharpFile.If("f");
        CSharpFile.ContinueIndented();
        CSharpFile.Return("g");
        CSharpFile.ReturnNoEnding("h");
        CSharpFile.Return();
        CSharpFile.Continue();

        Assert.Equal(
            "{\n" +
            "    if(a)\n        b();\n" +
            "    if(c)\n        return d;\n" +
            "    if(e)\n        return;\n" +
            "    if(f)\n        continue;\n" +
            "    return g;\n" +
            "    return h\n" +
            "    return;\n" +
            "    continue;\n", Text());
    }

    [Fact]
    public void ControlFlowIsIndentedWithItsBlock()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.ForEach("item", "items");
        CSharpFile.OpenBlock();
        CSharpFile.If("item == null");
        CSharpFile.ContinueIndented();
        CSharpFile.ElseIf("item.Done");
        CSharpFile.Break();
        CSharpFile.Else();
        CSharpFile.LineIndented("Work(item)");
        CSharpFile.CloseBlock();
        CSharpFile.While("Next()");
        CSharpFile.LineIndented("count++");
        CSharpFile.Do();
        CSharpFile.OpenBlock();
        CSharpFile.Line("count--");
        CSharpFile.CloseBlock();
        CSharpFile.DoWhile("count > 0");
        CSharpFile.Switch("kind");
        CSharpFile.OpenBlock();
        CSharpFile.Case("Kind.A");
        CSharpFile.ReturnIndented("1");
        CSharpFile.CaseDefault();
        CSharpFile.ReturnIndented("0");
        CSharpFile.CloseBlock();
        CSharpFile.CloseBlock();

        Assert.Equal(
            "{\n" +
            "    foreach(var item in items)\n" +
            "    {\n" +
            "        if(item == null)\n" +
            "            continue;\n" +
            "        else if(item.Done)\n" +
            "        break;\n" +
            "        else\n" +
            "            Work(item);\n" +
            "    }\n" +
            "    while(Next())\n" +
            "        count++;\n" +
            "    do\n" +
            "    {\n" +
            "        count--;\n" +
            "    }\n" +
            "    while(count > 0);\n" +
            "    switch(kind)\n" +
            "    {\n" +
            "        case Kind.A:\n" +
            "            return 1;\n" +
            "        default:\n" +
            "            return 0;\n" +
            "    }\n" +
            "}\n", Text());
    }

    [Fact]
    public void BlocksCloseWithTheirEnding()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithSemicolon();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithComma();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlockWithParenthesis();
        CSharpFile.OpenBlock();
        CSharpFile.CloseBlock("})");
        CSharpFile.CloseBlock();

        Assert.Equal("{\n    {\n    };\n    {\n    },\n    {\n    });\n    {\n    })\n}\n", Text());
    }

    [Fact]
    public void CommentsRegionsAndThrowsFollowTheIndent()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.OpenRegion("Reads");
        CSharpFile.Comment("one line");
        CSharpFile.Comment(new List<string> { "first", "second" });
        CSharpFile.ThrowArgumentException("\"bad\"");
        CSharpFile.ThrowNotSupportedException();
        CSharpFile.ThrowNotImplementedException();
        CSharpFile.CloseRegion();
        CSharpFile.Line("after");

        Assert.Equal(
            "{\n" +
            "    #region Reads\n" +
            "    // one line\n" +
            "    /*\n" +
            "        first\n" +
            "        second\n" +
            "*/\n" +
            "    throw new ArgumentException(\"bad\");\n" +
            "    throw new NotSupportedException(\"This feature is not supported.\");\n" +
            "    throw new NotImplementedException(\"This feature has not been implemented.\");\n" +
            "    #endregion\n" +
            "    after;\n", Text());
    }

    [Fact]
    public void UsingsAreWrittenOnceEachWithTheirAlias()
    {
        CSharpFile.Start(4);
        CSharpFile.UsingStatements(new List<UsingNamespace>
        {
            new UsingNamespace { Name = "System" },
            new UsingNamespace { Name = "System.Text" },
            new UsingNamespace { Name = "System" },
            new UsingNamespace { Name = "Microsoft.Data.SqlClient", Alias = "Sql", AliasRequired = true, AliasComment = " // the client" },
        });
        CSharpFile.OpenNamespace("My.Data");

        Assert.Equal("using System;\nusing System.Text;\nusing Sql = Microsoft.Data.SqlClient; // the client\n\nnamespace My.Data;\n", Text());
    }
    #endregion

    #region Declarations
    [Fact]
    public void AClassListsItsBaseAndInterfacesWithSpaces()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass(new ClassDeclaration { Name = "Repo", BaseClass = "Base<A, B>", Interfaces = new List<string> { "IRepo", "IOther" }, IsPartial = true });
        CSharpFile.CloseClass();
        CSharpFile.OpenClass(new ClassDeclaration { Name = "OnlyBase", BaseClass = "Base" });
        CSharpFile.CloseClass();
        CSharpFile.OpenClass(new ClassDeclaration { Name = "OnlyInterfaces", Interfaces = new List<string> { "IA", "IB", "IC" } });
        CSharpFile.CloseClass();
        CSharpFile.OpenClass(new ClassDeclaration { Name = "Nothing" });
        CSharpFile.CloseClass();

        Assert.Equal(
            "public partial class Repo : Base<A, B>, IRepo, IOther\n{\n}\n" +
            "public class OnlyBase : Base\n{\n}\n" +
            "public class OnlyInterfaces : IA, IB, IC\n{\n}\n" +
            "public class Nothing\n{\n}\n", Text());
    }

    [Fact]
    public void AClassWritesItsModifiersTypeParametersAndConstraints()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass(new ClassDeclaration
        {
            Name = "FilterBuilder",
            InheritanceModifier = InheritanceModifiers.Sealed,
            GenericParams = new GenericParameterList
            {
                new GenericParameter { Name = "TProps", Constraint = "PropertyContainer" },
                new GenericParameter { Name = "TValue" },
                new GenericParameter { Name = "TKey", Constraint = "struct" },
            },
        });
        CSharpFile.CloseClass();
        CSharpFile.OpenClass(new ClassDeclaration { Name = "Utils", IsStatic = true, IsPartial = true, AccessModifier = AccessModifiers.Internal });
        CSharpFile.CloseClass();

        Assert.Equal(
            "public sealed class FilterBuilder<TProps, TValue, TKey> where TProps : PropertyContainer where TKey : struct\n{\n}\n" +
            "internal static partial class Utils\n{\n}\n", Text());
    }

    [Fact]
    public void AnInterfaceListsItsInterfacesWithSpaces()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenInterface(new InterfaceDeclaration { Name = "INone" });
        CSharpFile.CloseInterface();
        CSharpFile.OpenInterface(new InterfaceDeclaration { Name = "IOne", Interfaces = new List<string> { "IA" } });
        CSharpFile.CloseInterface();
        CSharpFile.OpenInterface(new InterfaceDeclaration { Name = "IThree", Interfaces = new List<string> { "IA", "IB<int>", "IC" }, IsPartial = true });
        CSharpFile.CloseInterface();
        CSharpFile.OpenInterface(new InterfaceDeclaration
        {
            Name = "IQueryFilter",
            Interfaces = new List<string> { "IQueryFilter" },
            GenericParams = new GenericParameterList { new GenericParameter { Name = "TProps", Constraint = "PropertyContainer" } },
        });
        CSharpFile.CloseInterface();

        Assert.Equal(
            "public interface INone\n{\n}\n" +
            "public interface IOne : IA\n{\n}\n" +
            "public partial interface IThree : IA, IB<int>, IC\n{\n}\n" +
            "public interface IQueryFilter<TProps> : IQueryFilter where TProps : PropertyContainer\n{\n}\n", Text());
    }

    [Fact]
    public void AnEnumWritesItsBaseTypeAndMembers()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenEnum(new EnumDeclaration { Name = "Color", BaseType = TypeInfo.Int });
        CSharpFile.EnumMember(new EnumMemberDeclaration { Name = "Red", Value = "1" });
        CSharpFile.EnumMember(new EnumMemberDeclaration { Name = "Green" });
        CSharpFile.CloseEnum();
        CSharpFile.OpenEnum(new EnumDeclaration { Name = "Plain" });
        CSharpFile.CloseEnum();

        Assert.Equal("public enum Color : int\n{\n    Red = 1,\n    Green,\n}\npublic enum Plain\n{\n}\n", Text());
    }

    [Fact]
    public void PropertiesOfEveryKind()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Property(new PropertyDeclaration { Name = "Auto", ReturnType = TypeInfo.Int });
        CSharpFile.Property(new PropertyDeclaration { Name = "WithDefault", ReturnType = TypeInfo.String, DefaultValue = "\"x\"" });
        CSharpFile.Property(new PropertyDeclaration { Name = "PrivateSet", ReturnType = TypeInfo.Long, SetterModifier = AccessModifiers.Private });
        CSharpFile.Property(new PropertyDeclaration { Name = "ReadOnly", ReturnType = TypeInfo.Bool, Kind = PropertyKind.AutoNoSetter });
        CSharpFile.Property(new PropertyDeclaration { Name = "Total", ReturnType = TypeInfo.IntNullable, Kind = PropertyKind.Full });
        CSharpFile.Property(new PropertyDeclaration { Name = "Count", ReturnType = TypeInfo.Int, Kind = PropertyKind.Full, SetterModifier = AccessModifiers.Protected });
        CSharpFile.Property(new PropertyDeclaration { Name = "Root", ReturnType = new TypeInfo("Node"), Kind = PropertyKind.FullNoSetter, BackingField = "_root", AccessModifier = AccessModifiers.Internal });
        CSharpFile.Property(new PropertyDeclaration { Name = "Shared", ReturnType = TypeInfo.StringList, IsStatic = true });

        Assert.Equal(
            "{\n" +
            "    public int Auto { get; set; }\n" +
            "    public string WithDefault { get; set; } = \"x\";\n" +
            "    public long PrivateSet { get; private set; }\n" +
            "    public bool ReadOnly { get; }\n" +
            "    public int? Total { get{ return _total; } set{ _total = value; } }\n" +
            "    public int Count { get{ return _count; } protected set{ _count = value; } }\n" +
            "    internal Node Root { get{ return _root; } }\n" +
            "    public static List<string> Shared { get; set; }\n", Text());
    }

    [Fact]
    public void FieldsWriteTheirModifiersInOrder()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Field(new FieldDeclaration { Name = "_count", ReturnType = TypeInfo.Int, AccessModifier = AccessModifiers.Private, IsStatic = true, IsReadonly = true, DefaultValue = "0" });
        CSharpFile.Field(new FieldDeclaration { Name = "_names", ReturnType = TypeInfo.StringArray, AccessModifier = AccessModifiers.Private });

        Assert.Equal("{\n    private static readonly int _count = 0;\n    private string[] _names;\n", Text());
    }

    [Fact]
    public void MethodsWriteTheirSignature()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.DeclareMethod(new MethodDeclaration
        {
            Name = "Find",
            ReturnType = new TypeInfo("Region"),
            Parameters = new List<MethodParameter> { new MethodParameter { Name = "id", ParamType = "int" }, new MethodParameter { Name = "take", ParamType = "int", DefaultValue = "10" } },
        });
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "LoadAsync", ReturnType = TypeInfo.Int, IsAsync = true, AccessModifier = AccessModifiers.Protected, InheritanceModifier = InheritanceModifiers.Override });
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "PassAsync", ReturnType = TypeInfo.Int, IsAsync = true, SkipAwait = true });
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "RunAsync", ReturnType = TypeInfo.Task, IsAsync = true });
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "Shape", ReturnType = TypeInfo.Void, AccessModifier = AccessModifiers.Protected, InheritanceModifier = InheritanceModifiers.Abstract });
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "Hidden", ReturnType = TypeInfo.Void, InheritanceModifier = InheritanceModifiers.New, IsStatic = true });
        CSharpFile.DeclareMethod(new MethodDeclaration
        {
            Name = "New",
            IsStatic = true,
            ReturnType = new TypeInfo("IQueryFilter<TProps>"),
            GenericParams = new GenericParameterList { new GenericParameter { Name = "TProps", Constraint = "PropertyContainer" }, new GenericParameter { Name = "TValue" } },
            Parameters = new List<MethodParameter> { new MethodParameter { Name = "prop", ParamType = "Prop<TProps, TValue>" } },
        });

        Assert.Equal(
            "{\n" +
            "    public Region Find(int id, int take = 10)\n" +
            "    protected override async Task<int> LoadAsync()\n" +
            "    public Task<int> PassAsync()\n" +
            "    public async Task RunAsync()\n" +
            "    protected abstract void Shape();\n" +
            "    public static new void Hidden()\n" +
            "    public static IQueryFilter<TProps> New<TProps, TValue>(Prop<TProps, TValue> prop) where TProps : PropertyContainer\n", Text());
    }

    [Fact]
    public void InterfaceMembersHaveNoModifiersAndEndInASemicolon()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "FindAsync", ReturnType = new TypeInfo("Region"), IsAsync = true, IsInterfaceMember = true });
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "Count", ReturnType = TypeInfo.Int, IsInterfaceMember = true });
        CSharpFile.Property(new PropertyDeclaration { Name = "Region", ReturnType = new TypeInfo("IRegionRepository"), IsInterfaceMember = true });

        Assert.Equal("{\n    Task<Region> FindAsync();\n    int Count();\n    IRegionRepository Region { get; set; }\n", Text());
    }

    [Fact]
    public void ConstructorsWriteTheirParametersAndBaseCall()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Constructor(new ConstructorDeclaration
        {
            Name = "Repo",
            Parameters = new List<MethodParameter> { new MethodParameter { Name = "modelId", ParamType = "int" }, new MethodParameter { Name = "name", ParamType = "string" } },
            BaseConstructor = "base(modelId)",
        });
        CSharpFile.ConstructorEmpty(new ConstructorDeclaration { Name = "Empty", AccessModifier = AccessModifiers.Private });

        Assert.Equal("{\n    public Repo(int modelId, string name) : base(modelId)\n    private Empty()\n    {\n    }\n", Text());
    }
    #endregion

    #region Doc comments
    [Fact]
    public void AMethodDocumentsItsParametersAndReturns()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.DeclareMethod(new MethodDeclaration
        {
            Name = "ToList",
            Summary = new List<string> { "Reads the rows.", "Sorted." },
            ReturnType = new TypeInfo("Region", isList: true),
            Parameters = new List<MethodParameter> { new MethodParameter { Name = "query", ParamType = "Query", Description = "What to read." } },
        });

        Assert.Equal(
            "{\n" +
            "\n" +
            "    /// <summary>\n" +
            "    /// <para>Reads the rows.</para>\n" +
            "    /// <para>Sorted.</para>\n" +
            "    /// </summary>\n" +
            "    /// <param name=\"query\">What to read.</param>\n" +
            "    /// <returns>\n" +
            "    /// List of Region\n" +
            "    /// </returns>\n" +
            "    public List<Region> ToList(Query query)\n", Text());
    }

    [Theory]
    [InlineData("Region", "Region")]
    [InlineData("List<Region>", "List of Region")]
    [InlineData("Task<List<Region>>", "Task of List of Region")]
    [InlineData("Dictionary<string, List<int>>", "Dictionary of string, List of int")]
    [InlineData("StoredProcResult<GetEmployees_Result>", "StoredProcResult of GetEmployees_Result")]
    [InlineData("IQueryFilter<TProps>[]", "IQueryFilter of TProps[]")]
    [InlineData("<", " of ")]
    [InlineData(">", "")]
    [InlineData("A<<B>>", "A of  of B")]
    public void TheReturnsCommentReadsTheTypeInWords(string typeName, string words)
    {
        CSharpFile.Start(4);
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "M", Summary = new List<string> { "s" }, ReturnType = new TypeInfo(typeName) });

        Assert.Contains("/// <returns>\n/// " + words + "\n/// </returns>\n", Text(), StringComparison.Ordinal);
        //the same words as the text the comment used to be built from
        Assert.Equal(typeName.Replace("<", " of ").Replace(">", ""), words);
    }

    [Fact]
    public void TheReturnsCommentUsesTheGivenTextWhenThereIsOne()
    {
        CSharpFile.Start(4);
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "M", Summary = new List<string> { "s" }, ReturnType = new TypeInfo("List<Region>"), ReturnTypeForComments = "True if found <b>." });

        Assert.Contains("/// <returns>\n/// True if found <b>.\n/// </returns>\n", Text(), StringComparison.Ordinal);
    }

    [Fact]
    public void AnAsyncMethodDocumentsItsResultType()
    {
        CSharpFile.Start(4);
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "CountAsync", Summary = new List<string> { "s" }, ReturnType = TypeInfo.Int, IsAsync = true });

        Assert.Contains("/// <returns>\n/// int\n/// </returns>\npublic async Task<int> CountAsync()\n", Text(), StringComparison.Ordinal);
    }

    [Fact]
    public void APropertyDocumentsItsValueAndAFieldOnlyItsSummary()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Property(new PropertyDeclaration { Name = "Rows", Summary = new List<string> { "The rows." }, ReturnType = new TypeInfo("Region", isList: true) });
        CSharpFile.Property(new PropertyDeclaration { Name = "Size", Summary = new List<string> { "The size." }, ReturnType = TypeInfo.Int, ReturnTypeForComments = "How many rows." });
        CSharpFile.Field(new FieldDeclaration { Name = "_size", Summary = new List<string> { "Backs Size." }, ReturnType = TypeInfo.Int, AccessModifier = AccessModifiers.Private });

        Assert.Equal(
            "{\n" +
            "\n    /// <summary>\n    /// <para>The rows.</para>\n    /// </summary>\n    /// <value>\n    /// List of Region\n    /// </value>\n" +
            "    public List<Region> Rows { get; set; }\n" +
            "\n    /// <summary>\n    /// <para>The size.</para>\n    /// </summary>\n    /// <value>\n    /// How many rows.\n    /// </value>\n" +
            "    public int Size { get; set; }\n" +
            "\n    /// <summary>\n    /// <para>Backs Size.</para>\n    /// </summary>\n" +
            "    private int _size;\n", Text());
    }

    [Fact]
    public void AConstructorDocumentsItsParameters()
    {
        CSharpFile.Start(4);
        CSharpFile.Constructor(new ConstructorDeclaration
        {
            Name = "Repo",
            Summary = new List<string> { "Makes one." },
            Parameters = new List<MethodParameter> { new MethodParameter { Name = "id", ParamType = "int", Description = "The model." } },
        });

        Assert.Equal("\n/// <summary>\n/// <para>Makes one.</para>\n/// </summary>\n/// <param name=\"id\">The model.</param>\npublic Repo(int id)\n", Text());
    }

    [Fact]
    public void NoSummaryMeansNoDocComment()
    {
        CSharpFile.Start(4);
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "M", ReturnType = new TypeInfo("List<int>") });
        CSharpFile.Property(new PropertyDeclaration { Name = "P", ReturnType = TypeInfo.Int });
        CSharpFile.XmlSummary("");

        Assert.Equal("public List<int> M()\npublic int P { get; set; }\n", Text());
    }
    #endregion

    #region Header
    [Fact]
    public void TheHeaderShowsHowToExtendAGenericClass()
    {
        CSharpFile.Start(4);
        CSharpFile.Header(new ClassDeclaration
        {
            Name = "BatchEntry",
            Namespace = "My.Query",
            IsPartial = true,
            GenericParams = new GenericParameterList { new GenericParameter { Name = "TEntity", Constraint = "class" } },
        }, "/*SFF*/");

        Assert.Equal(
            "/*SFF*/\n" +
            "/************************************************\n" +
            "This Class has been generated dynamically.\n" +
            "Any changes you make here will be overwritten.\n" +
            "To add your own code create a partial Class in\n" +
            "a separate file with the following declaration:\n" +
            "\n" +
            "namespace My.Query;\n" +
            "public partial class BatchEntry<TEntity>\n" +
            "{\n" +
            "}\n" +
            "\n" +
            "************************************************/\n" +
            "\n", Text());
    }

    [Fact]
    public void TheHeaderNamesTheKindOfType()
    {
        CSharpFile.Start(4);
        CSharpFile.Header(new ClassDeclaration { Name = "Utils", Namespace = "A", IsPartial = true, IsStatic = true, AccessModifier = AccessModifiers.Internal }, "/*SFF*/");
        var forClass = Text();
        CSharpFile.Start(4);
        CSharpFile.Header(new InterfaceDeclaration { Name = "IQueryFilter", Namespace = "A", IsPartial = true }, "/*SFF*/");
        var forInterface = Text();
        CSharpFile.Start(4);
        CSharpFile.Header(new InterfaceDeclaration { Name = "IThing", Namespace = "A", IsPartial = true, GenericParams = new GenericParameterList { new GenericParameter { Name = "T" }, new GenericParameter { Name = "U" } } }, "/*SFF*/");
        var forGenericInterface = Text();

        Assert.Contains("\nnamespace A;\ninternal partial class Utils\n{\n}\n", forClass, StringComparison.Ordinal);
        Assert.Contains("This Interface has been generated dynamically.\n", forInterface, StringComparison.Ordinal);
        Assert.Contains("\nnamespace A;\npublic partial interface IQueryFilter\n{\n}\n", forInterface, StringComparison.Ordinal);
        Assert.Contains("\npublic partial interface IThing<T, U>\n{\n}\n", forGenericInterface, StringComparison.Ordinal);
    }

    [Fact]
    public void ATypeThatIsNotPartialGetsNoExample()
    {
        CSharpFile.Start(4);
        CSharpFile.Header(new EnumDeclaration { Name = "Color", Namespace = "A" }, "/*SFF*/");
        CSharpFile.Header(ArtifactKind.Class, "/*SFF*/");

        Assert.Equal(
            "/*SFF*/\n/************************************************\nThis Enum has been generated dynamically.\nAny changes you make here will be overwritten.\n************************************************/\n\n" +
            "/*SFF*/\n/************************************************\nThis Class has been generated dynamically.\nAny changes you make here will be overwritten.\n************************************************/\n\n", Text());
    }

    [Fact]
    public void TheFooterWrapsItsLinesAndIsLeftOutWhenEmpty()
    {
        CSharpFile.Start(4);
        CSharpFile.Footer();
        CSharpFile.Footer(new List<string>());
        CSharpFile.Footer(new List<string> { "one", "two" });

        Assert.Equal("/************************************************\none\ntwo\n************************************************/\n", Text());
    }
    #endregion

    #region Interpolated arguments
    private static readonly string _empty = "";

    private enum Kind
    {
        Value,
    }

    [Fact]
    public void InterpolatedArgumentsWriteTheSameTextAsStrings()
    {
        var withHandler = WriteInterpolated("count", 5);
        var withStrings = WriteStrings("count", 5);

        Assert.Equal(withStrings, withHandler);
        Assert.Equal(
            "{\n" +
            "    var count = 5;\n" +
            "    var count2 = 5;\n" +
            "    count(a,\n" +
            "    ;\n" +
            "    x = 5; // note\n" +
            "        count++;\n" +
            "    // count\n" +
            "    return count;\n" +
            "    return count\n" +
            "        return 5;\n" +
            "    var count3 = 5;\n" +
            "    // about count\n" +
            "    if(count > 5)\n" +
            "    else if(count < 5)\n" +
            "    while(count != 5)\n" +
            "    switch(count)\n" +
            "    case 5:\n" +
            "}\n", withHandler);
    }

    [Fact]
    public void InterpolatedArgumentsFormatLikeAnInterpolatedString()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            var number = 1234.5;
            var small = 7;
            var name = "ab";
            var nothing = (string)null;
            var flag = true;
            var kind = Kind.Value;
            var type = new SF.CodeGeneration.TypeInfo("Region", isList: true);

            CSharpFile.Start(4);
            CSharpFile.Line($"x = {number}, {number:N1}, [{small,4}], [{name,-4}], [{nothing}], {flag}, {kind}, {type}, {number.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            var withHandler = Text();

            var asString = $"x = {number}, {number:N1}, [{small,4}], [{name,-4}], [{nothing}], {flag}, {kind}, {type}, {number.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            CSharpFile.Start(4);
            CSharpFile.Line(asString);

            Assert.Equal(Text(), withHandler);
            Assert.Equal("x = 1234,5, 1.234,5, [   7], [ab  ], [], True, Value, List<Region>, 1234.5;\n", withHandler);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void AnInterpolatedArgumentIsWrittenWithoutAString()
    {
        //string holes: an int hole is boxed while the JIT still runs the method unoptimized, which says nothing about the writer
        var names = new string[200];
        var values = new string[200];
        for(var i = 0; i < 200; i++)
        {
            names[i] = "x" + i;
            values[i] = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        CSharpFile.Start(4);
        for(var i = 0; i < 200; i++)
            CSharpFile.Line($"var {names[i]} = {values[i]}");

        CSharpFile.Start(4);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for(var i = 0; i < 200; i++)
            CSharpFile.Line($"var {names[i]} = {values[i]}");
        var withHandler = GC.GetAllocatedBytesForCurrentThread() - before;

        CSharpFile.Start(4);
        before = GC.GetAllocatedBytesForCurrentThread();
        for(var i = 0; i < 200; i++)
            CSharpFile.Line(ComposeLine(i));
        var withStrings = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, withHandler);
        Assert.True(withStrings > 0, "the string lines allocate");
        Assert.Equal("var x199 = 199;\n", Text().Substring(Text().Length - "var x199 = 199;\n".Length));
    }

    [Fact]
    public void AnInterpolatedArgumentInsideAnotherOneFailsAndStartRecovers()
    {
        CSharpFile.Start(4);
        Assert.Throws<InvalidOperationException>(WriteNested);

        CSharpFile.Start(4);
        CSharpFile.Line($"c {1}");
        Assert.Equal("c 1;\n", Text());
    }

    private static void WriteNested()
    {
        CSharpFile.Line($"a {WriteInner()}");
    }

    private static string WriteInner()
    {
        CSharpFile.Line($"b {2}");
        return "x";
    }

    private static string ComposeLine(int i)
    {
        return "var x" + i + " = " + i;
    }

    private static string WriteInterpolated(string name, int count)
    {
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Line($"var {name} = {count}");
        CSharpFile.Line($"var {name}2 = {count};");
        CSharpFile.Line($"{name}(a,");
        CSharpFile.Line($"{_empty}");
        CSharpFile.Line($"x = {count}", "note");
        CSharpFile.LineIndented($"{name}++");
        CSharpFile.LineNoEnding($"// {name}");
        CSharpFile.Return($"{name}");
        CSharpFile.ReturnNoEnding($"{name}");
        CSharpFile.ReturnIndented($"{count}");
        CSharpFile.AppendIndented($"var {name}3 = ");
        CSharpFile.Append($"{count}");
        CSharpFile.CloseLine();
        CSharpFile.Comment($"about {name}");
        CSharpFile.If($"{name} > {count}");
        CSharpFile.ElseIf($"{name} < {count}");
        CSharpFile.While($"{name} != {count}");
        CSharpFile.Switch($"{name}");
        CSharpFile.Case($"{count}");
        CSharpFile.CloseBlock();
        return Text();
    }

    private static string WriteStrings(string name, int count)
    {
        var number = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        CSharpFile.Start(4);
        CSharpFile.OpenBlock();
        CSharpFile.Line(string.Concat("var ", name, " = ", number));
        CSharpFile.Line(string.Concat("var ", name, "2 = ", number, ";"));
        CSharpFile.Line(string.Concat(name, "(a,"));
        CSharpFile.Line(string.Concat(_empty, ""));
        CSharpFile.Line(string.Concat("x = ", number), "note");
        CSharpFile.LineIndented(string.Concat(name, "++"));
        CSharpFile.LineNoEnding(string.Concat("// ", name));
        CSharpFile.Return(string.Concat(name, ""));
        CSharpFile.ReturnNoEnding(string.Concat(name, ""));
        CSharpFile.ReturnIndented(string.Concat(number, ""));
        CSharpFile.AppendIndented(string.Concat("var ", name, "3 = "));
        CSharpFile.Append(string.Concat(number, ""));
        CSharpFile.CloseLine();
        CSharpFile.Comment(string.Concat("about ", name));
        CSharpFile.If(string.Concat(name, " > ", number));
        CSharpFile.ElseIf(string.Concat(name, " < ", number));
        CSharpFile.While(string.Concat(name, " != ", number));
        CSharpFile.Switch(string.Concat(name, ""));
        CSharpFile.Case(string.Concat(number, ""));
        CSharpFile.CloseBlock();
        return Text();
    }
    #endregion

    #region TypeInfo
    [Fact]
    public void TypeNamesShowNullableArrayAndList()
    {
        Assert.Equal("int", TypeInfo.Int.ToString());
        Assert.Equal("int?", TypeInfo.IntNullable.ToString());
        Assert.Equal("int[]", TypeInfo.IntArray.ToString());
        Assert.Equal("List<int>", TypeInfo.IntList.ToString());
        Assert.Equal("int?[]", TypeInfo.IntNullableArray.ToString());
        Assert.Equal("List<int?>", TypeInfo.IntNullableList.ToString());
        Assert.Equal("List<byte[]>", new TypeInfo("byte", isArray: true, isList: true).ToString());
        Assert.True(TypeInfo.Task.IsTask);
        Assert.True(TypeInfo.Void.IsVoid);
        Assert.False(new TypeInfo("Task<int>").IsTask);
    }
    #endregion

    private static string WriteSample()
    {
        CSharpFile.Start(4);
        CSharpFile.Header(new ClassDeclaration { Name = "Sample", Namespace = "A", IsPartial = true }, "/*SFF*/");
        CSharpFile.UsingStatements(new List<UsingNamespace> { new UsingNamespace { Name = "System" } });
        CSharpFile.OpenNamespace("A");
        CSharpFile.OpenBlock();
        CSharpFile.OpenClass(new ClassDeclaration { Name = "Sample", BaseClass = "Base<A, B>", Interfaces = new List<string> { "ISample" }, IsPartial = true });
        CSharpFile.DeclareMethod(new MethodDeclaration { Name = "Run", Summary = new List<string> { "Runs." }, ReturnType = new TypeInfo("List<int>") });
        CSharpFile.OpenBlock();
        CSharpFile.If("x");
        CSharpFile.ReturnIndented("null");
        CSharpFile.Return("new List<int>()");
        CSharpFile.CloseBlock();
        CSharpFile.CloseClass();
        CSharpFile.CloseBlock();
        return Text();
    }

    private static string Text()
    {
        return CSharpFile.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

#region Usings
using SF.CodeGeneration;
using Xunit;
#endregion

namespace SF.CSharpWriter.Tests;

/// <summary>
/// Type declarations: classes, structs, interfaces, records, enums and attributes. Each test starts a new file and
/// checks the exact text.
/// </summary>
public sealed class TypeTests
{
    #region Classes
    [Fact]
    public void ClassOpensAndCloses()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public sealed", "Person");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public sealed class Person", "{", "}"), CSharpFile.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ClassWithoutModifiersStartsWithTheKeyword(string modifiers)
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass(modifiers, "Person");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("class Person", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void ClassBodyIsIndented()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Person");
        CSharpFile.Field("private", "int", "_id");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public class Person", "{", "    private int _id;", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void ClassWritesItsBaseList()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Customer", "Person, IDisposable");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public class Customer : Person, IDisposable", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void ClassWritesItsConstraintsOnTheirOwnLine()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Repository<T>", "IRepository<T>", "where T : class, new()");
        CSharpFile.CloseType();

        var expected = Code.Lines("public class Repository<T> : IRepository<T>", "    where T : class, new()", "{", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ClassWritesSeveralWhereClausesAsGiven()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Map<TKey, TValue>", null, "where TKey : notnull where TValue : struct");
        CSharpFile.CloseType();

        var expected = Code.Lines("public class Map<TKey, TValue>", "    where TKey : notnull where TValue : struct", "{", "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void ClassWithAPrimaryConstructorPassesItToTheBase()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Service(ILogger logger)", "Base(logger)");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public class Service(ILogger logger) : Base(logger)", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void NestedClassIsIndentedWithItsConstraints()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public", "Outer");
        CSharpFile.OpenClass("private sealed", "Inner<T>", null, "where T : struct");
        CSharpFile.CloseType();
        CSharpFile.CloseType();

        var expected = Code.Lines(
            "public class Outer",
            "{",
            "    private sealed class Inner<T>",
            "        where T : struct",
            "    {",
            "    }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void IndentSizeIsHonored()
    {
        CSharpFile.Start(2);
        CSharpFile.OpenClass("public", "Outer");
        CSharpFile.OpenClass("public", "Inner<T>", null, "where T : class");
        CSharpFile.Field("public", "T", "Value");
        CSharpFile.CloseType();
        CSharpFile.CloseType();

        var expected = Code.Lines(
            "public class Outer",
            "{",
            "  public class Inner<T>",
            "    where T : class",
            "  {",
            "    public T Value;",
            "  }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion

    #region Structs and Interfaces
    [Fact]
    public void StructOpensWithItsBaseList()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenStruct("public readonly", "Point", "IEquatable<Point>");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public readonly struct Point : IEquatable<Point>", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void StructWritesItsConstraints()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenStruct("public", "Pair<T>", null, "where T : unmanaged");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public struct Pair<T>", "    where T : unmanaged", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void InterfaceOpensWithItsConstraints()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenInterface("public", "IRepository<T>", "IDisposable", "where T : class");
        CSharpFile.AutoProperty("", "int", "Count", "get;");
        CSharpFile.CloseType();

        var expected = Code.Lines(
            "public interface IRepository<T> : IDisposable",
            "    where T : class",
            "{",
            "    int Count { get; }",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }
    #endregion

    #region Records
    [Fact]
    public void RecordWithABodyOpens()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenRecord("public", "Person(string Name)", "Entity");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public record Person(string Name) : Entity", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void RecordStructWithABodyOpens()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenRecordStruct("public readonly", "Range(int Start, int End)");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public readonly record struct Range(int Start, int End)", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void RecordWithABodyWritesItsConstraints()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenRecord("public", "Box<T>(T Value)", null, "where T : class");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public record Box<T>(T Value)", "    where T : class", "{", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void RecordWithoutABodyIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.Record("public", "Person(string Name, int Age)");

        Assert.Equal(Code.Lines("public record Person(string Name, int Age);"), CSharpFile.ToString());
    }

    [Fact]
    public void RecordWithoutABodyEndsItsConstraintsWithTheSemicolon()
    {
        CSharpFile.Start(4);
        CSharpFile.Record("public", "Box<T>(T Value)", "Container", "where T : class");

        Assert.Equal(Code.Lines("public record Box<T>(T Value) : Container", "    where T : class;"), CSharpFile.ToString());
    }

    [Fact]
    public void RecordStructWithoutABodyIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.RecordStruct("public readonly", "Point(int X, int Y)");

        Assert.Equal(Code.Lines("public readonly record struct Point(int X, int Y);"), CSharpFile.ToString());
    }

    [Fact]
    public void RecordWithoutABodyInsideAClassIsIndented()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenClass("public static", "Messages");
        CSharpFile.Record("public", "Created(int Id)");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public static class Messages", "{", "    public record Created(int Id);", "}"), CSharpFile.ToString());
    }
    #endregion

    #region Enums
    [Fact]
    public void EnumWritesItsMembers()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenEnum("public", "Color");
        CSharpFile.EnumMember("Red");
        CSharpFile.EnumMember("Green", "2");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("public enum Color", "{", "    Red,", "    Green = 2,", "}"), CSharpFile.ToString());
    }

    [Fact]
    public void EnumWritesItsUnderlyingType()
    {
        CSharpFile.Start(4);
        CSharpFile.OpenEnum("internal", "Flags", "byte");
        CSharpFile.EnumMember("None", "0");
        CSharpFile.CloseType();

        Assert.Equal(Code.Lines("internal enum Flags : byte", "{", "    None = 0,", "}"), CSharpFile.ToString());
    }
    #endregion

    #region Delegates
    [Fact]
    public void DelegateIsOneStatement()
    {
        CSharpFile.Start(4);
        CSharpFile.Delegate("public", "void", "Changed", "object sender, string name");

        Assert.Equal(Code.Lines("public delegate void Changed(object sender, string name);"), CSharpFile.ToString());
    }

    [Fact]
    public void DelegateEndsItsConstraintsWithTheSemicolon()
    {
        CSharpFile.Start(4);
        CSharpFile.Delegate("public", "T", "Factory<T>", "int id", "where T : class");

        Assert.Equal(Code.Lines("public delegate T Factory<T>(int id)", "    where T : class;"), CSharpFile.ToString());
    }

    [Fact]
    public void DelegateWithoutParameters()
    {
        CSharpFile.Start(4);
        CSharpFile.Delegate("internal", "int", "Counter", null);

        Assert.Equal(Code.Lines("internal delegate int Counter();"), CSharpFile.ToString());
    }
    #endregion

    #region Attributes
    [Fact]
    public void AttributeIsWrittenAtTheCurrentIndentation()
    {
        CSharpFile.Start(4);
        CSharpFile.Attribute("Serializable");
        CSharpFile.OpenClass("public", "Person");
        CSharpFile.Attribute("Obsolete(\"Use Customer\")");
        CSharpFile.Field("public", "int", "Id");
        CSharpFile.CloseType();

        var expected = Code.Lines(
            "[Serializable]",
            "public class Person",
            "{",
            "    [Obsolete(\"Use Customer\")]",
            "    public int Id;",
            "}");
        Assert.Equal(expected, CSharpFile.ToString());
    }

    [Fact]
    public void AttributeKeepsItsTarget()
    {
        CSharpFile.Start(4);
        CSharpFile.Attribute("assembly: InternalsVisibleTo(\"Tests\")");

        Assert.Equal(Code.Lines("[assembly: InternalsVisibleTo(\"Tests\")]"), CSharpFile.ToString());
    }
    #endregion
}

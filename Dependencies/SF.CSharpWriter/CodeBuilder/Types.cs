namespace SF.CodeGeneration;

//type declarations: classes, structs, interfaces, records, enums and delegates. Every Open method writes the
//declaration and '{' and indents what follows; close the type with CloseType. name is everything between the keyword
//and the base list, so it carries the type parameters and a primary constructor ("Repository<T>(string connection)");
//modifiers is the text before the keyword ("public sealed partial"), empty or null for none
public static partial class CSharpFile
{
    #region Classes, Structs and Interfaces
    /// <summary>
    /// Closes a type opened with one of the Open methods: '}'.
    /// </summary>
    public static void CloseType()
    {
        CloseBlock();
    }

    /// <summary>
    /// Opens a class: <c>modifiers class name : baseList</c>, the where clauses on their own line, then '{'.
    /// </summary>
    /// <param name="baseList">What follows the ':' ("Base(name), IDisposable"); null for none.</param>
    /// <param name="constraints">The where clauses ("where T : class"); null for none.</param>
    public static void OpenClass(string modifiers, string name, string baseList = null, string constraints = null)
    {
        OpenType(modifiers, "class", name, baseList, constraints);
    }

    /// <summary>
    /// Opens a struct: <c>modifiers struct name : baseList</c>, the where clauses on their own line, then '{'.
    /// </summary>
    public static void OpenStruct(string modifiers, string name, string baseList = null, string constraints = null)
    {
        OpenType(modifiers, "struct", name, baseList, constraints);
    }

    /// <summary>
    /// Opens an interface: <c>modifiers interface name : baseList</c>, the where clauses on their own line, then '{'.
    /// </summary>
    public static void OpenInterface(string modifiers, string name, string baseList = null, string constraints = null)
    {
        OpenType(modifiers, "interface", name, baseList, constraints);
    }
    #endregion

    #region Records
    /// <summary>
    /// Opens a record class with a body: <c>modifiers record name : baseList</c>, then '{'.
    /// </summary>
    public static void OpenRecord(string modifiers, string name, string baseList = null, string constraints = null)
    {
        OpenType(modifiers, "record", name, baseList, constraints);
    }

    /// <summary>
    /// Opens a record struct with a body: <c>modifiers record struct name : baseList</c>, then '{'.
    /// </summary>
    public static void OpenRecordStruct(string modifiers, string name, string baseList = null, string constraints = null)
    {
        OpenType(modifiers, "record struct", name, baseList, constraints);
    }

    /// <summary>
    /// A positional record class without a body, one statement: <c>public record Person(string Name);</c>
    /// </summary>
    public static void Record(string modifiers, string name, string baseList = null, string constraints = null)
    {
        TypeWithoutBody(modifiers, "record", name, baseList, constraints);
    }

    /// <summary>
    /// A positional record struct without a body, one statement: <c>public record struct Point(int X, int Y);</c>
    /// </summary>
    public static void RecordStruct(string modifiers, string name, string baseList = null, string constraints = null)
    {
        TypeWithoutBody(modifiers, "record struct", name, baseList, constraints);
    }
    #endregion

    #region Enums
    /// <summary>
    /// Opens an enum: <c>modifiers enum name : underlyingType</c>, then '{'. Write its members with EnumMember.
    /// </summary>
    /// <param name="underlyingType">"byte", "long"...; null for int.</param>
    public static void OpenEnum(string modifiers, string name, string underlyingType = null)
    {
        OpenType(modifiers, "enum", name, underlyingType, null);
    }

    /// <summary>
    /// An enum member: <c>Name,</c> (the comma after the last member is valid C#).
    /// </summary>
    public static void EnumMember(string name)
    {
        AppendIndent().Append(name).AppendLine(",");
    }

    /// <summary>
    /// An enum member with its value: <c>Name = value,</c>
    /// </summary>
    public static void EnumMember(string name, string value)
    {
        AppendIndent().Append(name).Append(" = ").Append(value).AppendLine(",");
    }
    #endregion

    #region Delegates
    /// <summary>
    /// A delegate type: <c>modifiers delegate returnType name(parameters);</c>, the where clauses before the ';'.
    /// </summary>
    public static void Delegate(string modifiers, string returnType, string name, string parameters, string constraints = null)
    {
        AppendDeclarationStart(modifiers)
          .Append("delegate ")
          .Append(returnType)
          .Append(' ')
          .Append(name)
          .Append('(')
          .Append(parameters)
          .Append(')');
        AppendClause(constraints);
        _b.AppendLine(";");
    }
    #endregion

    #region Attributes
    /// <summary>
    /// An attribute on its own line, at the current indentation: <c>[attribute]</c>. A target goes in the text
    /// ("assembly: InternalsVisibleTo(\"Tests\")").
    /// </summary>
    public static void Attribute(string attribute)
    {
        AppendIndent().Append('[').Append(attribute).AppendLine("]");
    }
    #endregion

    #region Declaration Helpers
    private static void OpenType(string modifiers, string keyword, string name, string baseList, string constraints)
    {
        AppendTypeDeclaration(modifiers, keyword, name, baseList);
        AppendClause(constraints);
        _b.AppendLine();
        OpenBlock();
    }

    private static void TypeWithoutBody(string modifiers, string keyword, string name, string baseList, string constraints)
    {
        AppendTypeDeclaration(modifiers, keyword, name, baseList);
        AppendClause(constraints);
        _b.AppendLine(";");
    }

    private static void AppendTypeDeclaration(string modifiers, string keyword, string name, string baseList)
    {
        AppendDeclarationStart(modifiers).Append(keyword).Append(' ').Append(name);
        if(HasText(baseList))
            _b.Append(" : ").Append(baseList);
    }
    #endregion
}

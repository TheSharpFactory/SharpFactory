namespace SF.CodeGeneration;

//member declarations: fields, constants, properties, methods, constructors, indexers, events and operators.
//modifiers is the text before the type ("private static readonly"), empty or null for none; parameters is the text
//between the parentheses, written as given ("int id, ref string name, int count = 0"), empty or null for none. Open
//methods write the declaration and '{' and indent what follows; close the member with CloseBlock
public static partial class CSharpFile
{
    #region Fields and Constants
    /// <summary>
    /// A field: <c>modifiers type name;</c>
    /// </summary>
    public static void Field(string modifiers, string type, string name)
    {
        AppendDeclarationStart(modifiers).Append(type).Append(' ').Append(name);
        EndStatement(name);
    }

    /// <summary>
    /// A field with its initial value: <c>modifiers type name = initializer;</c>
    /// </summary>
    public static void Field(string modifiers, string type, string name, string initializer)
    {
        AppendDeclarationStart(modifiers)
          .Append(type)
          .Append(' ')
          .Append(name)
          .Append(" = ")
          .Append(initializer);
        EndStatement(initializer);
    }

    /// <summary>
    /// A constant: <c>modifiers const type name = value;</c>
    /// </summary>
    /// <param name="value">The value as C# ("10", "\"text\"", "1.5m").</param>
    public static void Const(string modifiers, string type, string name, string value)
    {
        AppendDeclarationStart(modifiers)
          .Append("const ")
          .Append(type)
          .Append(' ')
          .Append(name)
          .Append(" = ")
          .Append(value);
        EndStatement(value);
    }
    #endregion

    #region Properties
    /// <summary>
    /// An auto property with a getter and a setter: <c>modifiers type name { get; set; }</c>
    /// </summary>
    public static void AutoProperty(string modifiers, string type, string name)
    {
        AutoProperty(modifiers, type, name, "get; set;");
    }

    /// <summary>
    /// An auto property with the given accessors: <c>modifiers type name { accessors }</c>
    /// ("get;", "get; init;", "get; private set;"). Also the property of an interface.
    /// </summary>
    public static void AutoProperty(string modifiers, string type, string name, string accessors)
    {
        AppendAutoProperty(modifiers, type, name, accessors);
        _b.AppendLine();
    }

    /// <summary>
    /// An auto property with its initial value: <c>modifiers type name { accessors } = initializer;</c>
    /// </summary>
    public static void AutoProperty(string modifiers, string type, string name, string accessors, string initializer)
    {
        AppendAutoProperty(modifiers, type, name, accessors);
        _b.Append(" = ").Append(initializer);
        EndStatement(initializer);
    }

    /// <summary>
    /// A read-only property with an expression body: <c>modifiers type name => expression;</c>
    /// </summary>
    public static void ExpressionProperty(string modifiers, string type, string name, string expression)
    {
        AppendDeclarationStart(modifiers)
          .Append(type)
          .Append(' ')
          .Append(name)
          .Append(" => ")
          .Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// Opens a property with accessors of its own: <c>modifiers type name</c>, then '{'. Write the accessors with
    /// Get, Set, Init or Accessor (or their Open forms) and close it with CloseBlock.
    /// </summary>
    public static void OpenProperty(string modifiers, string type, string name)
    {
        AppendDeclarationStart(modifiers).Append(type).Append(' ').Append(name);
        _b.AppendLine();
        OpenBlock();
    }

    /// <summary>
    /// A getter with an expression body: <c>get => expression;</c>
    /// </summary>
    public static void Get(string expression)
    {
        Accessor("get", expression);
    }

    /// <summary>
    /// A setter with an expression body: <c>set => expression;</c>
    /// </summary>
    public static void Set(string expression)
    {
        Accessor("set", expression);
    }

    /// <summary>
    /// An init accessor with an expression body: <c>init => expression;</c>
    /// </summary>
    public static void Init(string expression)
    {
        Accessor("init", expression);
    }

    /// <summary>
    /// Any accessor with an expression body: <c>accessor => expression;</c> ("private set", "protected init").
    /// </summary>
    public static void Accessor(string accessor, string expression)
    {
        AppendIndent().Append(accessor).Append(" => ").Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// Opens a getter with a body: <c>get</c>, then '{'. Close it with CloseBlock.
    /// </summary>
    public static void OpenGet()
    {
        OpenAccessor("get");
    }

    /// <summary>
    /// Opens a setter with a body: <c>set</c>, then '{'. Close it with CloseBlock.
    /// </summary>
    public static void OpenSet()
    {
        OpenAccessor("set");
    }

    /// <summary>
    /// Opens an init accessor with a body: <c>init</c>, then '{'. Close it with CloseBlock.
    /// </summary>
    public static void OpenInit()
    {
        OpenAccessor("init");
    }

    /// <summary>
    /// Opens any accessor with a body ("private set"), then '{'. Close it with CloseBlock.
    /// </summary>
    public static void OpenAccessor(string accessor)
    {
        AppendIndent().AppendLine(accessor);
        OpenBlock();
    }

    private static void AppendAutoProperty(string modifiers, string type, string name, string accessors)
    {
        AppendDeclarationStart(modifiers)
          .Append(type)
          .Append(' ')
          .Append(name)
          .Append(" { ")
          .Append(accessors)
          .Append(" }");
    }
    #endregion

    #region Methods
    /// <summary>
    /// Opens a method: <c>modifiers returnType name(parameters)</c>, the where clauses on their own line, then '{'.
    /// <paramref name="name"/> carries the type parameters ("Find&lt;T&gt;").
    /// </summary>
    /// <param name="constraints">The where clauses ("where T : class"); null for none.</param>
    public static void OpenMethod(string modifiers, string returnType, string name, string parameters, string constraints = null)
    {
        AppendMethodSignature(modifiers, returnType, name, parameters, constraints);
        _b.AppendLine();
        OpenBlock();
    }

    /// <summary>
    /// A method with an expression body: <c>modifiers returnType name(parameters) => expression;</c>
    /// </summary>
    public static void ExpressionMethod(string modifiers, string returnType, string name, string parameters, string expression, string constraints = null)
    {
        AppendMethodSignature(modifiers, returnType, name, parameters, constraints);
        _b.Append(" => ").Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// A method without a body: <c>modifiers returnType name(parameters);</c> (interface, abstract, partial or extern).
    /// </summary>
    public static void MethodSignature(string modifiers, string returnType, string name, string parameters, string constraints = null)
    {
        AppendMethodSignature(modifiers, returnType, name, parameters, constraints);
        _b.AppendLine(";");
    }

    private static void AppendMethodSignature(string modifiers, string returnType, string name, string parameters, string constraints)
    {
        AppendDeclarationStart(modifiers)
          .Append(returnType)
          .Append(' ')
          .Append(name)
          .Append('(')
          .Append(parameters)
          .Append(')');
        AppendClause(constraints);
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Opens a constructor: <c>modifiers name(parameters)</c>, the initializer on its own line (<c>: base(...)</c>),
    /// then '{'. A static constructor has the modifier "static".
    /// </summary>
    /// <param name="initializer">"base(name)" or "this(0)", without the ':'; null for none.</param>
    public static void OpenConstructor(string modifiers, string name, string parameters, string initializer = null)
    {
        AppendDeclarationStart(modifiers).Append(name).Append('(').Append(parameters).Append(')');
        AppendClause(": ", initializer);
        _b.AppendLine();
        OpenBlock();
    }

    /// <summary>
    /// A constructor with an expression body: <c>modifiers name(parameters) => expression;</c>
    /// </summary>
    public static void ExpressionConstructor(string modifiers, string name, string parameters, string expression)
    {
        AppendDeclarationStart(modifiers)
          .Append(name)
          .Append('(')
          .Append(parameters)
          .Append(") => ")
          .Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// Opens a finalizer: <c>~name()</c>, then '{'.
    /// </summary>
    public static void OpenFinalizer(string name)
    {
        AppendIndent().Append('~').Append(name).AppendLine("()");
        OpenBlock();
    }
    #endregion

    #region Indexers
    /// <summary>
    /// Opens an indexer: <c>modifiers type this[parameters]</c>, then '{'. Write its accessors as for a property.
    /// </summary>
    public static void OpenIndexer(string modifiers, string type, string parameters)
    {
        AppendIndexer(modifiers, type, parameters);
        _b.AppendLine();
        OpenBlock();
    }

    /// <summary>
    /// A read-only indexer with an expression body: <c>modifiers type this[parameters] => expression;</c>
    /// </summary>
    public static void ExpressionIndexer(string modifiers, string type, string parameters, string expression)
    {
        AppendIndexer(modifiers, type, parameters);
        _b.Append(" => ").Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// An indexer without bodies: <c>modifiers type this[parameters] { accessors }</c> (interface or abstract).
    /// </summary>
    public static void IndexerSignature(string modifiers, string type, string parameters, string accessors)
    {
        AppendIndexer(modifiers, type, parameters);
        _b.Append(" { ").Append(accessors).AppendLine(" }");
    }

    private static void AppendIndexer(string modifiers, string type, string parameters)
    {
        AppendDeclarationStart(modifiers).Append(type).Append(" this[").Append(parameters).Append(']');
    }
    #endregion

    #region Events
    /// <summary>
    /// A field-like event: <c>modifiers event type name;</c>
    /// </summary>
    public static void Event(string modifiers, string type, string name)
    {
        AppendDeclarationStart(modifiers).Append("event ").Append(type).Append(' ').Append(name);
        EndStatement(name);
    }

    /// <summary>
    /// Opens an event with accessors of its own: <c>modifiers event type name</c>, then '{'. Write them with Add and
    /// Remove (or their Open forms) and close it with CloseBlock.
    /// </summary>
    public static void OpenEvent(string modifiers, string type, string name)
    {
        AppendDeclarationStart(modifiers).Append("event ").Append(type).Append(' ').Append(name);
        _b.AppendLine();
        OpenBlock();
    }

    /// <summary>
    /// An add accessor with an expression body: <c>add => expression;</c>
    /// </summary>
    public static void Add(string expression)
    {
        Accessor("add", expression);
    }

    /// <summary>
    /// A remove accessor with an expression body: <c>remove => expression;</c>
    /// </summary>
    public static void Remove(string expression)
    {
        Accessor("remove", expression);
    }

    /// <summary>
    /// Opens an add accessor with a body: <c>add</c>, then '{'.
    /// </summary>
    public static void OpenAdd()
    {
        OpenAccessor("add");
    }

    /// <summary>
    /// Opens a remove accessor with a body: <c>remove</c>, then '{'.
    /// </summary>
    public static void OpenRemove()
    {
        OpenAccessor("remove");
    }
    #endregion

    #region Operators
    /// <summary>
    /// Opens an operator: <c>modifiers returnType operator op(parameters)</c>, then '{'
    /// ("public static", "Money", "+", "Money left, Money right").
    /// </summary>
    public static void OpenOperator(string modifiers, string returnType, string op, string parameters)
    {
        AppendOperator(modifiers, returnType, op, parameters);
        _b.AppendLine();
        OpenBlock();
    }

    /// <summary>
    /// An operator with an expression body: <c>modifiers returnType operator op(parameters) => expression;</c>
    /// </summary>
    public static void ExpressionOperator(string modifiers, string returnType, string op, string parameters, string expression)
    {
        AppendOperator(modifiers, returnType, op, parameters);
        _b.Append(" => ").Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// An operator without a body: <c>modifiers returnType operator op(parameters);</c> (static abstract in an interface).
    /// </summary>
    public static void OperatorSignature(string modifiers, string returnType, string op, string parameters)
    {
        AppendOperator(modifiers, returnType, op, parameters);
        _b.AppendLine(";");
    }

    /// <summary>
    /// Opens a conversion operator: <c>modifiers kind operator type(parameters)</c>, then '{'
    /// ("public static", "implicit", "decimal", "Money value").
    /// </summary>
    /// <param name="kind">"implicit" or "explicit".</param>
    public static void OpenConversion(string modifiers, string kind, string type, string parameters)
    {
        AppendOperator(modifiers, kind, type, parameters);
        _b.AppendLine();
        OpenBlock();
    }

    /// <summary>
    /// A conversion operator with an expression body: <c>modifiers kind operator type(parameters) => expression;</c>
    /// </summary>
    public static void ExpressionConversion(string modifiers, string kind, string type, string parameters, string expression)
    {
        AppendOperator(modifiers, kind, type, parameters);
        _b.Append(" => ").Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// <c>modifiers before operator op(parameters)</c>: before is the return type, or implicit/explicit.
    /// </summary>
    private static void AppendOperator(string modifiers, string before, string op, string parameters)
    {
        AppendDeclarationStart(modifiers)
          .Append(before)
          .Append(" operator ")
          .Append(op)
          .Append('(')
          .Append(parameters)
          .Append(')');
    }
    #endregion
}

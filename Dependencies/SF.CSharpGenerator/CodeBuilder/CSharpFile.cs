#region Usings
using System.Collections.Generic;
using System.Text;
#endregion

namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    private static readonly Dictionary<AccessModifiers, string> _accessModifiers = new Dictionary<AccessModifiers, string>(7)
    {
        { AccessModifiers.Public, AccessModifierConstants.Public },
        { AccessModifiers.Private, AccessModifierConstants.Private },
        { AccessModifiers.Protected, AccessModifierConstants.Protected },
        { AccessModifiers.Internal, AccessModifierConstants.Internal },
        { AccessModifiers.ProtectedInternal, AccessModifierConstants.ProtectedInternal },
        { AccessModifiers.PrivateProtected, AccessModifierConstants.PrivateProtected },
        { AccessModifiers.None, AccessModifierConstants.None },
    };

    private static readonly Dictionary<InheritanceModifiers, string> _inheritanceModifiers = new Dictionary<InheritanceModifiers, string>(7)
    {
        { InheritanceModifiers.Abstract, InheritanceModifierConstants.Abstract },
        { InheritanceModifiers.Sealed, InheritanceModifierConstants.Sealed },
        { InheritanceModifiers.Override, InheritanceModifierConstants.Override },
        { InheritanceModifiers.Virtual, InheritanceModifierConstants.Virtual },
        { InheritanceModifiers.New, InheritanceModifierConstants.New },
        { InheritanceModifiers.None, InheritanceModifierConstants.None },
    };

    private static readonly StringBuilder _b = new StringBuilder(100000);

    private static int _indentSize = 4;
    private static string _currentIndent = "";
    private static string _baseIndent = "";

    /// <summary>
    /// Start a new code file. Call this every time you are going to start a new code file.
    /// </summary>
    /// <param name="indentSize">Sets the indentation size.</param>
    public static void Start(int indentSize)
    {
        _indentSize = indentSize;
        Start();
    }

    /// <summary>
    /// <para>Start a new code file. Call this every time you are going to start a new code file.</para>
    /// <para>Keeps the current Indent Size or uses the default if not explicitly set.</para>
    /// </summary>
    public static void Start()
    {
        _b.Length = 0;

        _baseIndent = new string(' ', _indentSize);

        _currentIndent = "";
    }

    public static void Append(string val)
    {
        _b.Append(val);
    }

    public static void Append(char val)
    {
        _b.Append(val);
    }

    /// <summary>
    /// <para>Appends to the current file respecting the current indentation.</para>
    /// <para>Does not create a new line.</para>
    /// </summary>
    /// <param name="val">The value to append</param>
    public static void AppendIndented(string val)
    {
        _b.Append(_currentIndent).Append(val);
    }
    public static void Replace(string oldValue, string newValue)
    {
        _b.Replace(oldValue, newValue);
    }

    #region All Types Helpers
    public static void CloseTypeWithNamespace()
    {
        CloseBlock();
        CloseNamespace();
    }
    public static void CloseType()
    {
        CloseBlock();
    }
    #endregion

    #region Member Helpers
    private static void MemberSignature(IComplexDeclaration member, bool isReadOnly = false)
    {
        _b.Append(_currentIndent);

        var hideMember = member.InheritanceModifier == InheritanceModifiers.New;

        var isMethod = member.Artifact == ArtifactKind.Method;

        if(!member.IsInterfaceMember)
        {
            if(isMethod && member.IsPartial)
            {
                _b.Append("partial ");
            }
            else
            {
                _b.Append(_accessModifiers[member.AccessModifier]);

                if(member.IsStatic)
                    _b.Append(" static");

                if(hideMember)
                    _b.Append(" new");

                if(isReadOnly)
                    _b.Append(" readonly");

                if(!hideMember && member.InheritanceModifier != InheritanceModifiers.None)
                    _b.Append(' ').Append(_inheritanceModifiers[member.InheritanceModifier]);

                _b.Append(' ');
            }
        }

        if(isMethod && ((IMethodDeclaration)member).IsAsync)
        {
            if(!member.IsInterfaceMember)
            {
                _b.Append("async")
                  .Append(' ');
            }

            if(!member.ReturnType.IsVoid && !member.ReturnType.IsTask)
            {
                _b.Append("Task<")
                  .Append(member.ReturnType)
                  .Append('>');
            }
            else
            {
                _b.Append(member.ReturnType);
            }

            _b.Append(' ')
              .Append(member.Name);
        }
        else
        {
            _b.Append(member.ReturnType)
              .Append(' ')
              .Append(member.Name);
        }
    }
    #endregion

    #region Generic Type Helpers
    private static void ComposeGenericParams(IGenericDeclaration type)
    {
        if(!type.IsGeneric)
            return;

        _b.Append('<');

        foreach(var g in type.GenericParams)
            _b.Append(g.Name).Append(',');

        _b.Length--;//remove last unused comma
        _b.Append('>');
    }
    private static void ComposeGenericConstraints(IGenericDeclaration type)
    {
        if(!type.IsGeneric)
            return;

        foreach(var g in type.GenericParams)
        {
            if(string.IsNullOrWhiteSpace(g.Constraint))
                continue;

            _b.Append(" where ").Append(g.Name).Append(" : ").Append(g.Constraint);
        }
    }
    #endregion

    public static void Return(string val)
    {
        _b.Append(_currentIndent).Append("return ").Append(val);
        AddSemicolon(val);
        _b.AppendLine();
    }
    public static void ReturnNoEnding(string val)
    {
        _b.Append(_currentIndent).Append("return ").Append(val);
        _b.AppendLine();
    }
    public static void ReturnIndented(string val)
    {
        _b.Append(_baseIndent);
        Return(val);
    }
    public static void Return()
    {
        _b.Append(_currentIndent).AppendLine("return;");
    }
    public static void ReturnIndented()
    {
        _b.Append(_baseIndent);
        Return();
    }

    public static void Continue()
    {
        _b.Append(_currentIndent).AppendLine("continue;");
    }
    public static void ContinueIndented()
    {
        _b.Append(_baseIndent);
        Continue();
    }

    public static new string ToString()
    {
        return _b.ToString();
    }
}

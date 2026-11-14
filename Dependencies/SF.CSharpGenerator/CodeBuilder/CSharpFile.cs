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
    //how many blocks deep the next line is: its spaces are written straight into the file, no indent strings
    private static int _indentLevel = 0;

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
        _indentLevel = 0;
        //an interpolated argument left over from a file that failed halfway
        _pending.Length = 0;
        _pendingInUse = false;
    }

    /// <summary>
    /// Writes the current indentation (the spaces of every open block).
    /// </summary>
    private static StringBuilder AppendIndent()
    {
        return _b.Append(' ', _indentLevel * _indentSize);
    }

    /// <summary>
    /// Writes one more level of indentation (a single statement under an if, foreach or while without braces).
    /// </summary>
    private static StringBuilder AppendIndentStep()
    {
        return _b.Append(' ', _indentSize);
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
        AppendIndent().Append(val);
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
        AppendIndent();

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
                //each modifier with the space after it; None (an explicit interface implementation) writes nothing
                if(member.AccessModifier != AccessModifiers.None)
                    _b.Append(_accessModifiers[member.AccessModifier]).Append(' ');

                if(member.IsStatic)
                    _b.Append("static ");

                if(hideMember)
                    _b.Append("new ");

                if(isReadOnly)
                    _b.Append("readonly ");

                if(!hideMember && member.InheritanceModifier != InheritanceModifiers.None)
                    _b.Append(_inheritanceModifiers[member.InheritanceModifier]).Append(' ');
            }
        }

        if(isMethod && ((IMethodDeclaration)member).IsAsync)
        {
            if(!member.IsInterfaceMember && !((IMethodDeclaration)member).SkipAwait)
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
            _b.Append(g.Name).Append(',').Append(' ');

        _b.Length -= 2;//remove the last unused comma and space
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
        AppendIndent().Append("return ").Append(val);
        AddSemicolon(val);
        _b.AppendLine();
    }
    public static void ReturnNoEnding(string val)
    {
        AppendIndent().Append("return ").Append(val);
        _b.AppendLine();
    }
    public static void ReturnIndented(string val)
    {
        AppendIndentStep();
        Return(val);
    }
    public static void Return()
    {
        AppendIndent().AppendLine("return;");
    }
    public static void ReturnIndented()
    {
        AppendIndentStep();
        Return();
    }

    public static void Continue()
    {
        AppendIndent().AppendLine("continue;");
    }
    public static void ContinueIndented()
    {
        AppendIndentStep();
        Continue();
    }

    public static new string ToString()
    {
        return _b.ToString();
    }
}

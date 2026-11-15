namespace SF.CodeGeneration;

//statements: returns, loops, exception handling, resources, locks, iterators, jumps and checked or unsafe blocks. Like
//If and While, a statement that has a body writes only its own line: open its block with OpenBlock, or write a single
//statement under it with LineIndented
public static partial class CSharpFile
{
    #region Return and Continue
    /// <summary>
    /// <c>return expression;</c>
    /// </summary>
    public static void Return(string expression)
    {
        AppendIndent().Append("return ").Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// <c>return;</c>
    /// </summary>
    public static void Return()
    {
        AppendIndent().AppendLine("return;");
    }

    /// <summary>
    /// <c>return expression</c> with no semicolon: the expression goes on over the next lines.
    /// </summary>
    public static void ReturnNoEnding(string expression)
    {
        AppendIndent().Append("return ").AppendLine(expression);
    }

    /// <summary>
    /// <c>return expression;</c> one level deeper: the single statement under an if without braces.
    /// </summary>
    public static void ReturnIndented(string expression)
    {
        AppendIndentStep();
        Return(expression);
    }

    /// <summary>
    /// <c>return;</c> one level deeper.
    /// </summary>
    public static void ReturnIndented()
    {
        AppendIndentStep();
        Return();
    }

    /// <summary>
    /// <c>continue;</c>
    /// </summary>
    public static void Continue()
    {
        AppendIndent().AppendLine("continue;");
    }

    /// <summary>
    /// <c>continue;</c> one level deeper.
    /// </summary>
    public static void ContinueIndented()
    {
        AppendIndentStep();
        Continue();
    }
    #endregion

    #region Loops
    /// <summary>
    /// <c>for(initializer; condition; iterator)</c>. Empty parts are left out: <c>for(;;)</c>.
    /// </summary>
    public static void For(string initializer, string condition, string iterator)
    {
        AppendIndent().Append("for(").Append(initializer).Append(';');
        if(HasText(condition))
            _b.Append(' ').Append(condition);

        _b.Append(';');
        if(HasText(iterator))
            _b.Append(' ').Append(iterator);

        _b.AppendLine(")");
    }

    /// <summary>
    /// <c>foreach(var variable in collection)</c>; a deconstruction goes in the variable ("(key, value)").
    /// </summary>
    public static void ForEach(string variable, string collection)
    {
        ForEach("var", variable, collection);
    }

    /// <summary>
    /// A foreach with the type of its variable: <c>foreach(type variable in collection)</c>.
    /// </summary>
    public static void ForEach(string type, string variable, string collection)
    {
        AppendIndent()
          .Append("foreach(")
          .Append(type)
          .Append(' ')
          .Append(variable)
          .Append(" in ")
          .Append(collection)
          .AppendLine(")");
    }

    /// <summary>
    /// <c>await foreach(var variable in collection)</c>.
    /// </summary>
    public static void AwaitForEach(string variable, string collection)
    {
        AwaitForEach("var", variable, collection);
    }

    /// <summary>
    /// <c>await foreach(type variable in collection)</c>.
    /// </summary>
    public static void AwaitForEach(string type, string variable, string collection)
    {
        AppendIndent()
          .Append("await foreach(")
          .Append(type)
          .Append(' ')
          .Append(variable)
          .Append(" in ")
          .Append(collection)
          .AppendLine(")");
    }
    #endregion

    #region Exception Handling
    /// <summary>
    /// <c>try</c>; open its block next.
    /// </summary>
    public static void Try()
    {
        AppendIndent().AppendLine("try");
    }

    /// <summary>
    /// A catch of every exception: <c>catch</c>.
    /// </summary>
    public static void Catch()
    {
        AppendIndent().AppendLine("catch");
    }

    /// <summary>
    /// <c>catch(declaration)</c>: "Exception ex", or the type alone ("IOException").
    /// </summary>
    public static void Catch(string declaration)
    {
        AppendIndent().Append("catch(").Append(declaration).AppendLine(")");
    }

    /// <summary>
    /// A catch with a filter: <c>catch(declaration) when(filter)</c>.
    /// </summary>
    public static void Catch(string declaration, string filter)
    {
        AppendIndent().Append("catch(").Append(declaration).Append(") when(").Append(filter).AppendLine(")");
    }

    /// <summary>
    /// <c>finally</c>; open its block next.
    /// </summary>
    public static void Finally()
    {
        AppendIndent().AppendLine("finally");
    }

    /// <summary>
    /// <c>throw expression;</c>
    /// </summary>
    public static void Throw(string expression)
    {
        AppendIndent().Append("throw ").Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// Throws the caught exception again: <c>throw;</c>
    /// </summary>
    public static void Rethrow()
    {
        AppendIndent().AppendLine("throw;");
    }
    #endregion

    #region Resources and Locks
    /// <summary>
    /// A using statement with a body: <c>using(resource)</c> ("var stream = File.OpenRead(path)").
    /// </summary>
    public static void Using(string resource)
    {
        AppendIndent().Append("using(").Append(resource).AppendLine(")");
    }

    /// <summary>
    /// A using declaration, disposed at the end of the block: <c>using declaration;</c> ("var stream = ...").
    /// </summary>
    public static void UsingDeclaration(string declaration)
    {
        AppendIndent().Append("using ").Append(declaration);
        EndStatement(declaration);
    }

    /// <summary>
    /// <c>await using(resource)</c>.
    /// </summary>
    public static void AwaitUsing(string resource)
    {
        AppendIndent().Append("await using(").Append(resource).AppendLine(")");
    }

    /// <summary>
    /// <c>await using declaration;</c>
    /// </summary>
    public static void AwaitUsingDeclaration(string declaration)
    {
        AppendIndent().Append("await using ").Append(declaration);
        EndStatement(declaration);
    }

    /// <summary>
    /// <c>lock(expression)</c>.
    /// </summary>
    public static void Lock(string expression)
    {
        AppendIndent().Append("lock(").Append(expression).AppendLine(")");
    }
    #endregion

    #region Iterators
    /// <summary>
    /// <c>yield return expression;</c>
    /// </summary>
    public static void YieldReturn(string expression)
    {
        AppendIndent().Append("yield return ").Append(expression);
        EndStatement(expression);
    }

    /// <summary>
    /// <c>yield break;</c>
    /// </summary>
    public static void YieldBreak()
    {
        AppendIndent().AppendLine("yield break;");
    }
    #endregion

    #region Switch and Jumps
    /// <summary>
    /// A case with a guard: <c>case pattern when condition:</c>
    /// </summary>
    public static void CaseWhen(string pattern, string condition)
    {
        AppendIndent().Append("case ").Append(pattern).Append(" when ").Append(condition).AppendLine(":");
    }

    /// <summary>
    /// <c>goto case value;</c>
    /// </summary>
    public static void GotoCase(string value)
    {
        AppendIndent().Append("goto case ").Append(value);
        EndStatement(value);
    }

    /// <summary>
    /// <c>goto default;</c>
    /// </summary>
    public static void GotoDefault()
    {
        AppendIndent().AppendLine("goto default;");
    }

    /// <summary>
    /// <c>goto label;</c>
    /// </summary>
    public static void Goto(string label)
    {
        AppendIndent().Append("goto ").Append(label);
        EndStatement(label);
    }

    /// <summary>
    /// A label for goto, at the current indentation: <c>label:</c>
    /// </summary>
    public static void Label(string label)
    {
        AppendIndent().Append(label).AppendLine(":");
    }
    #endregion

    #region Checked and Unsafe
    /// <summary>
    /// <c>checked</c>; open its block next.
    /// </summary>
    public static void Checked()
    {
        AppendIndent().AppendLine("checked");
    }

    /// <summary>
    /// <c>unchecked</c>; open its block next.
    /// </summary>
    public static void Unchecked()
    {
        AppendIndent().AppendLine("unchecked");
    }

    /// <summary>
    /// <c>unsafe</c>; open its block next.
    /// </summary>
    public static void Unsafe()
    {
        AppendIndent().AppendLine("unsafe");
    }

    /// <summary>
    /// <c>fixed(declaration)</c> ("byte* first = buffer").
    /// </summary>
    public static void Fixed(string declaration)
    {
        AppendIndent().Append("fixed(").Append(declaration).AppendLine(")");
    }
    #endregion
}

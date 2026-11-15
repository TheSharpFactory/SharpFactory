namespace SF.CodeGeneration;

//the using directives at the top of a file
public static partial class CSharpFile
{
    /// <summary>
    /// <c>using name;</c>
    /// </summary>
    public static void UsingNamespace(string name)
    {
        AppendIndent().Append("using ").Append(name);
        EndStatement(name);
    }

    /// <summary>
    /// <c>using static type;</c>
    /// </summary>
    public static void UsingStatic(string type)
    {
        AppendIndent().Append("using static ").Append(type);
        EndStatement(type);
    }

    /// <summary>
    /// <c>using alias = target;</c>
    /// </summary>
    public static void UsingAlias(string alias, string target)
    {
        AppendIndent().Append("using ").Append(alias).Append(" = ").Append(target);
        EndStatement(target);
    }

    /// <summary>
    /// <c>global using name;</c>
    /// </summary>
    public static void GlobalUsing(string name)
    {
        AppendIndent().Append("global using ").Append(name);
        EndStatement(name);
    }
}

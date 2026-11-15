namespace SF.CodeGeneration;

//indentation by hand, for code that is not a block (the statements under a case, a continued expression)
public static partial class CSharpFile
{
    /// <summary>
    /// What follows is one level deeper.
    /// </summary>
    public static void Indent()
    {
        _indentLevel++;
    }

    /// <summary>
    /// What follows is one level less deep; never past the left margin.
    /// </summary>
    public static void Outdent()
    {
        if(_indentLevel > 0)
            _indentLevel--;
    }
}

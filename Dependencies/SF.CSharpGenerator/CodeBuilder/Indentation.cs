namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void Indent()
    {
        _indentLevel++;
    }
    public static void Outdent()
    {
        //never below the left margin
        if(_indentLevel > 0)
            _indentLevel--;
    }
}

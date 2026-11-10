namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void Indent()
    {
        _currentIndent += _baseIndent;
    }
    public static void Outdent()
    {
        var outdentSize = _currentIndent.Length - _indentSize;
        if(outdentSize < 1)
            _currentIndent = "";
        else
            _currentIndent = _currentIndent.Substring(0, outdentSize);
    }
}

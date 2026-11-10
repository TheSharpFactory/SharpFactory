namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void OpenRegion(string regionName)
    {
        _b.Append(_currentIndent).Append("#region ").AppendLine(regionName);
    }
    public static void CloseRegion()
    {
        _b.Append(_currentIndent).AppendLine("#endregion");
    }
}

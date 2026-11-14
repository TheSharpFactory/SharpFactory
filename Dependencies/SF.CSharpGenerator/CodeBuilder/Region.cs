namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    public static void OpenRegion(string regionName)
    {
        AppendIndent().Append("#region ").AppendLine(regionName);
    }
    public static void CloseRegion()
    {
        AppendIndent().AppendLine("#endregion");
    }
}

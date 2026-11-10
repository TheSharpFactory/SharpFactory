namespace SF.CodeGeneration;

public sealed class MethodParameter
{
    public string Name { get; set; }
    public string ParamType { get; set; }
    public string Description { get; set; } = "";
    public string DefaultValue { get; set; }
}

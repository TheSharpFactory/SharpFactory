namespace SF.CodeGeneration;

public sealed class GenericParameter
{
    public string Name { get; set; }
    public string Constraint { get; set; }

    public override string ToString()
    {
        return Name;
    }
}

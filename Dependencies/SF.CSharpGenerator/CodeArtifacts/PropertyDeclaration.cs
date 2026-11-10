namespace SF.CodeGeneration;

public sealed class PropertyDeclaration : ComplexDeclarationBase
{
    public override ArtifactKind Artifact
    {
        get
        {
            return ArtifactKind.Property;
        }
    }

    /// <summary>
    /// Default Auto
    /// </summary>
    public PropertyKind Kind { get; set; } = PropertyKind.Auto;

    public AccessModifiers SetterModifier { get; set; } = AccessModifiers.None;

    public string DefaultValue { get; set; }

    public string BackingField { get; set; }
}

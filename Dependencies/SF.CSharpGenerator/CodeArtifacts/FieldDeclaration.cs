namespace SF.CodeGeneration;

public sealed class FieldDeclaration : ComplexDeclarationBase
{
    public override ArtifactKind Artifact
    {
        get
        {
            return ArtifactKind.Field;
        }
    }

    public FieldDeclaration()
    {
        AccessModifier = AccessModifiers.Private;
    }

    /// <summary>
    /// Default is None
    /// </summary>
    public AccessModifiers SetterModifier { get; set; } = AccessModifiers.None;

    public string DefaultValue { get; set; }

    public bool IsReadonly { get; set; }
}

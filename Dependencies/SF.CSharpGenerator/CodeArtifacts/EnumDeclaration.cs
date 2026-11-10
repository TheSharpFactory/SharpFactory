namespace SF.CodeGeneration;

public sealed class EnumDeclaration : DeclarationBase
{
    public override ArtifactKind Artifact
    {
        get
        {
            return ArtifactKind.Enum;
        }
    }

    public TypeInfo BaseType { get; set; }
}

namespace SF.CodeGeneration;

public sealed class InterfaceDeclaration : PolymorphicDeclarationBase
{
    public override ArtifactKind Artifact
    {
        get
        {
            return ArtifactKind.Interface;
        }
    }

    public new AccessModifiers AccessModifier { get; private set; } = AccessModifiers.Public;
}

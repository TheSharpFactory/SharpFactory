namespace SF.CodeGeneration;

public sealed class EnumMemberDeclaration : DeclarationBase
{
    public override ArtifactKind Artifact
    {
        get
        {
            return ArtifactKind.EnumMember;
        }
    }

    public string Value { get; set; }
}

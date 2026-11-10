#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public abstract class DeclarationBase : IDeclaration
{
    public abstract ArtifactKind Artifact { get; }

    /// <summary>
    /// Default is Private for fields and Public for all other artifacts.
    /// </summary>
    public AccessModifiers AccessModifier { get; set; } = AccessModifiers.Public;
    public string Name { get; set; }
    public List<string> Summary { get; set; }
    /// <summary>
    /// Default is false.
    /// </summary>
    public bool IsPartial { get; set; }
    public string Namespace { get; set; }
}

#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public interface IDeclaration
{
    ArtifactKind Artifact { get; }
    AccessModifiers AccessModifier { get; set; }
    string Name { get; set; }
    List<string> Summary { get; set; }
    bool IsPartial { get; set; }
    string Namespace { get; set; }
}

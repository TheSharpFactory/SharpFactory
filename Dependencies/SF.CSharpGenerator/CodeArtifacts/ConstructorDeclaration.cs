#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public sealed class ConstructorDeclaration : ComplexDeclarationBase
{
    public override ArtifactKind Artifact
    {
        get
        {
            return ArtifactKind.Constructor;
        }
    }

    public string BaseConstructor { get; set; }

    public List<MethodParameter> Parameters { get; set; }

    public string GenerateCall()
    {
        var parameters = "";

        if(Parameters?.Count > 0)
        {
            parameters = Parameters[0].Name;
            if(Parameters.Count > 1)
            {
                for(var i = 1; i < Parameters.Count; i++)
                {
                    parameters += $", {Parameters[i].Name}";
                }
            }
        }

        return $"new {Name}({parameters})";
    }
}

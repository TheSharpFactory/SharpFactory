#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public sealed class MethodDeclaration : PolymorphicDeclarationBase, IMethodDeclaration
{
    public override ArtifactKind Artifact
    {
        get
        {
            return ArtifactKind.Method;
        }
    }

    public List<MethodParameter> Parameters { get; set; }
    public bool IsAsync { get; set; }
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

        var awaitCall = IsAsync ? "await " : "";
        return $"{awaitCall}{Name}({parameters})";
    }
}

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
    /// <summary>
    /// An async method whose body returns the awaited call's Task instead of awaiting it: declared without the async keyword.
    /// </summary>
    public bool SkipAwait { get; set; }
    public string GenerateCall()
    {
        return GenerateCall(false);
    }
    /// <summary>
    /// The call to this method; skipAwait writes it without await (the caller returns the Task).
    /// </summary>
    public string GenerateCall(bool skipAwait)
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

        var awaitCall = IsAsync && !skipAwait ? "await " : "";
        return $"{awaitCall}{Name}({parameters})";
    }
}

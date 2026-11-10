#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public abstract class PolymorphicDeclarationBase : ComplexDeclarationBase, IGenericDeclaration, IPolymorphicDeclaration
{
    /// <summary>
    /// List: interfaces.
    /// </summary>
    public List<string> Interfaces { get; set; }
    public bool IsGeneric { get { return GenericParams?.Count > 0; } }

    /// <summary>
    /// True if Interfaces is not null or empty.
    /// </summary>
    public bool ImplementsInterfaces { get { return Interfaces?.Count > 0; } }
    public GenericParameterList GenericParams { get; set; }
}

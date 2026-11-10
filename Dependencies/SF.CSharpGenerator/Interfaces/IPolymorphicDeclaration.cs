#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public interface IPolymorphicDeclaration : IComplexDeclaration
{
    bool ImplementsInterfaces { get; }

    /// <summary>
    /// List: interfaces.
    /// </summary>
    List<string> Interfaces { get; set; }
}

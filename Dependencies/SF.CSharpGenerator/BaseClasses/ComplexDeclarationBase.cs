#region Usings
using System.Collections.Generic;
#endregion

namespace SF.CodeGeneration;

public abstract class ComplexDeclarationBase : DeclarationBase, IComplexDeclaration
{
    public InheritanceModifiers InheritanceModifier { get; set; } = InheritanceModifiers.None;
    public bool IsStatic { get; set; }
    public bool IsInterfaceMember { get; set; }
    public TypeInfo ReturnType { get; set; } = TypeInfo.Void;
    public string ReturnTypeForComments { get; set; }
    public List<string> Attributes { get; set; }
}

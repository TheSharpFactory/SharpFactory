namespace SF.CodeGeneration;

public interface IComplexDeclaration : IDeclaration
{
    InheritanceModifiers InheritanceModifier { get; set; }
    bool IsStatic { get; set; }
    bool IsInterfaceMember { get; set; }
    TypeInfo ReturnType { get; set; }
    string ReturnTypeForComments { get; set; }
}

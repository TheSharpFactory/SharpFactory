namespace SF.CodeGeneration;

public interface IGenericDeclaration
{
    bool IsGeneric { get; }
    GenericParameterList GenericParams { get; set; }
}

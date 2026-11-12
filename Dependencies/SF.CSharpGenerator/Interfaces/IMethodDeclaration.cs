namespace SF.CodeGeneration;

internal interface IMethodDeclaration
{
    bool IsAsync { get; set; }
    bool SkipAwait { get; set; }
}

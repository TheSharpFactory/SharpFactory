namespace SF.CodeGeneration;

public sealed class ClassDeclaration : PolymorphicDeclarationBase
{
    public override ArtifactKind Artifact
    {
        get
        {
            return ArtifactKind.Class;
        }
    }

    public string BaseClass { get; set; }

    public bool IsDerived { get { return !string.IsNullOrWhiteSpace(BaseClass); } }

    private TypeInfo _returnType = null;
    public new TypeInfo ReturnType
    {
        get
        {
            if(_returnType == null)
            {
                var type = Name;

                if(IsGeneric)
                {
                    type += "<";
                    type += GenericParams.ToString();
                    type += ">";
                }

                _returnType = new TypeInfo(type);
            }

            return _returnType;
        }
        private set { }
    }
}

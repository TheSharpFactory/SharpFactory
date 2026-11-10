namespace SF.Models;

public sealed class IdentityComputedGeneratedInfo
{
    //this class is not meant to be bound. therefore it is not implementing INotifyPropertyChange
    public string Entity { get; set; } = null;
    public string Property { get; set; } = null;
    public bool IsIdentity { get; set; } = false;
    public bool IsComputed { get; set; } = false;
    public bool IsGenerated { get; set; } = false;

    public IdentityComputedGeneratedInfo Clone()
    {
        var result = new IdentityComputedGeneratedInfo
        {
            Entity = Entity,
            Property = Property,
            IsIdentity = IsIdentity,
            IsComputed = IsComputed,
            IsGenerated = IsGenerated
        };
        return result;
    }
}

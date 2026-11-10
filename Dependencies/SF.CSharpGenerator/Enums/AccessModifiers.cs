namespace SF.CodeGeneration;

/// <summary>
/// Available values:
/// <para>Public</para>
/// <para>Private</para>
/// <para>Protected</para>
/// <para>Internal</para>
/// <para>ProtectedInternal</para>
/// <para>PrivateProtected</para>
/// <para>None</para>
/// </summary>
public enum AccessModifiers : int
{
    /// <summary>
    /// The type or member can be accessed by any other code in the same assembly or another assembly that references it.
    /// </summary>
    Public = 0,

    /// <summary>
    /// The type or member can be accessed only by code in the same class or struct.
    /// </summary>
    Private = 1,

    /// <summary>
    /// The type or member can be accessed only by code in the same class, or in a class that is derived from that class.
    /// </summary>
    Protected = 2,

    /// <summary>
    /// The type or member can be accessed by any code in the same assembly, but not from another assembly.
    /// </summary>
    Internal = 3,

    /// <summary>
    /// The type or member can be accessed by any code in the assembly in which it is declared, or from within a derived class in another assembly.
    /// </summary>
    ProtectedInternal = 4,

    /// <summary>
    /// The type or member can be accessed only within its declaring assembly, by code in the same class or in a type that is derived from that class.
    /// </summary>
    PrivateProtected = 5,

    /// <summary>
    /// Used in interfaces.  No Access Modifier.
    /// </summary>
    None = 7,
}

internal static class AccessModifierConstants
{
    /// <summary>
    /// The type or member can be accessed by any other code in the same assembly or another assembly that references it.
    /// </summary>
    public const string Public = "public";

    /// <summary>
    /// The type or member can be accessed only by code in the same class or struct.
    /// </summary>
    public const string Private = "private";

    /// <summary>
    /// The type or member can be accessed only by code in the same class, or in a class that is derived from that class.
    /// </summary>
    public const string Protected = "protected";

    /// <summary>
    /// The type or member can be accessed by any code in the same assembly, but not from another assembly.
    /// </summary>
    public const string Internal = "internal";

    /// <summary>
    /// The type or member can be accessed by any code in the assembly in which it is declared, or from within a derived class in another assembly.
    /// </summary>
    public const string ProtectedInternal = "protected internal";

    /// <summary>
    /// The type or member can be accessed only within its declaring assembly, by code in the same class or in a type that is derived from that class.
    /// </summary>
    public const string PrivateProtected = "private protected";

    /// <summary>
    /// Used in interfaces.  No Access Modifier.
    /// </summary>
    public const string None = "";
}

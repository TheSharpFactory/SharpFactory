namespace SF.CodeGeneration;

/// <summary>
/// Available values:
/// <para>Abstract</para>
/// <para>Sealed</para>
/// <para>Virtual</para>
/// <para>Override</para>
/// <para>New</para>  
/// </summary>
public enum InheritanceModifiers : int
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// The abstract modifier indicates that the thing being modified has a missing or incomplete implementation.
    /// The abstract modifier can be used with classes, methods, properties, indexers, and events.
    /// Use the abstract modifier in a class declaration to indicate that a class is intended only to be a base class of other classes, not instantiated on its own.
    /// Members marked as abstract must be implemented by non-abstract classes that derive from the abstract class.
    /// </summary>
    Abstract = 1,

    /// <summary>
    /// When applied to a class, the sealed modifier prevents other classes from inheriting from it.
    /// In the following example, class B inherits from class A, but no class can inherit from class B.
    /// You can also use the sealed modifier on a method or property that overrides a virtual method or property in a base class.
    /// This enables you to allow classes to derive from your class and prevent them from overriding specific virtual methods or properties.
    /// </summary>
    Sealed = 2,

    /// <summary>
    /// The virtual keyword is used to modify a method, property, indexer, or event declaration and allow for it to be overridden in a derived class.
    /// </summary>
    Virtual = 3,

    /// <summary>
    /// The override modifier is required to extend or modify the abstract or virtual implementation of an inherited method, property, indexer, or event.
    /// </summary>
    Override = 4,

    /// <summary>
    /// When used as a declaration modifier, the new keyword explicitly hides a member that is inherited from a base class.
    /// When you hide an inherited member, the derived version of the member replaces the base class version.
    /// Although you can hide members without using the new modifier, you get a compiler warning.
    /// If you use new to explicitly hide a member, it suppresses this warning.
    /// </summary>
    New = 5
}

internal static class InheritanceModifierConstants
{
    /// <summary>
    /// The abstract modifier indicates that the thing being modified has a missing or incomplete implementation.
    /// The abstract modifier can be used with classes, methods, properties, indexers, and events.
    /// Use the abstract modifier in a class declaration to indicate that a class is intended only to be a base class of other classes, not instantiated on its own.
    /// Members marked as abstract must be implemented by non-abstract classes that derive from the abstract class.
    /// </summary>
    public const string Abstract = "abstract";

    /// <summary>
    /// When applied to a class, the sealed modifier prevents other classes from inheriting from it.
    /// In the following example, class B inherits from class A, but no class can inherit from class B.
    /// You can also use the sealed modifier on a method or property that overrides a virtual method or property in a base class.
    /// This enables you to allow classes to derive from your class and prevent them from overriding specific virtual methods or properties.
    /// </summary>
    public const string Sealed = "sealed";

    /// <summary>
    /// The virtual keyword is used to modify a method, property, indexer, or event declaration and allow for it to be overridden in a derived class.
    /// </summary>
    public const string Virtual = "virtual";

    /// <summary>
    /// The override modifier is required to extend or modify the abstract or virtual implementation of an inherited method, property, indexer, or event.
    /// </summary>
    public const string Override = "override";

    /// <summary>
    /// When used as a declaration modifier, the new keyword explicitly hides a member that is inherited from a base class.
    /// When you hide an inherited member, the derived version of the member replaces the base class version.
    /// Although you can hide members without using the new modifier, you get a compiler warning.
    /// If you use new to explicitly hide a member, it suppresses this warning.
    /// </summary>
    public const string New = "new";

    /// <summary>
    /// None.
    /// </summary>
    public const string None = "";
}

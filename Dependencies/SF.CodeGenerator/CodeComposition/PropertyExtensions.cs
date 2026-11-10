#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

internal static partial class PropertyExtensions
{
    /// <summary>      
    /// If no Access Modifier is specified it defaults to Public.
    /// Has both setter and getter.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="propAccessModifier"></param>
    /// <param name="isStatic"></param>
    /// <param name="description"></param>
    /// /// <param name="defaultValue"></param>
    public static void Auto(TypeInfo type, string name, AccessModifiers propAccessModifier = AccessModifiers.Public, bool isStatic = false, string description = "", string defaultValue = null)
    {
        Write(type, name, description, PropertyKind.Auto, propAccessModifier, AccessModifiers.None, isStatic, false, null, defaultValue);
    }
    /// <summary>
    /// If no Access Modifier is specified it defaults to Public.
    /// Has only getter.
    /// </summary>     
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="propAccessModifier"></param>  
    /// <param name="isStatic"></param>
    /// <param name="description"></param>
    public static void AutoNoSetter(TypeInfo type, string name, AccessModifiers propAccessModifier = AccessModifiers.Public, bool isStatic = false, string description = "")
    {
        Write(type, name, description, PropertyKind.AutoNoSetter, propAccessModifier, AccessModifiers.None, isStatic, false);
    }
    /// <summary>
    /// If no Access Modifier is specified it defaults to Public.
    /// Has a getter and a private setter.
    /// </summary>     
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="propAccessModifier"></param>  
    /// <param name="isStatic"></param>
    /// <param name="description"></param>
    public static void AutoPrivateSetter(TypeInfo type, string name, AccessModifiers propAccessModifier = AccessModifiers.Public, bool isStatic = false, string description = "")
    {
        Write(type, name, description, PropertyKind.Auto, propAccessModifier, AccessModifiers.Private, isStatic, false);
    }
    /// <summary>
    /// If no Access Modifier is specified it defaults to Public.
    /// Has a getter and a protected setter.
    /// </summary>     
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="propAccessModifier"></param>  
    /// <param name="isStatic"></param>
    /// <param name="description"></param>
    public static void AutoProtectedSetter(TypeInfo type, string name, AccessModifiers propAccessModifier = AccessModifiers.Public, bool isStatic = false, string description = "")
    {
        Write(type, name, description, PropertyKind.Auto, propAccessModifier, AccessModifiers.Protected, isStatic, false);
    }

    /// <summary>
    /// If no backing field is specified the name of the backing field will the same as the property using lower dash + camelCase.
    /// If no Access Modifier is specified it defaults to Public.
    /// </summary>     
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="setterAccessModifier"></param>
    /// <param name="propAccessModifier"></param>
    /// <param name="backingField"></param>
    /// <param name="isStatic"></param>
    /// <param name="description"></param>
    public static void Full(TypeInfo type, string name, AccessModifiers propAccessModifier, AccessModifiers setterAccessModifier, string backingField = null, bool isStatic = false, string description = null)
    {
        if(string.IsNullOrWhiteSpace(backingField))
            backingField = "_" + Inflector.Camelize(name);

        Write(type, name, description, PropertyKind.Full, propAccessModifier, setterAccessModifier, isStatic, false, backingField);
    }

    /// <summary>
    /// If no backing field is specified the name of the backing field will the same as the property using lower dash + camelCase.
    /// If no Access Modifier is specified it defaults to Public.
    /// </summary>     
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="propAccessModifier"></param>
    /// <param name="backingField"></param>
    /// <param name="isStatic"></param>
    /// <param name="description"></param>
    public static void FullNoSetter(TypeInfo type, string name, AccessModifiers propAccessModifier = AccessModifiers.Public, string backingField = null, bool isStatic = false, string description = null)
    {
        if(string.IsNullOrWhiteSpace(backingField))
            backingField = "_" + Inflector.Camelize(name);

        Write(type, name, description, PropertyKind.FullNoSetter, propAccessModifier, AccessModifiers.None, isStatic, false, backingField);
    }

    public static void InterfaceAuto(TypeInfo type, string name, string description = "")
    {
        Write(type, name, description, PropertyKind.Auto, AccessModifiers.None, AccessModifiers.None, false, true);
    }
    public static void InterfaceAutoNoSetter(TypeInfo type, string name, string description = "")
    {
        Write(type, name, description, PropertyKind.AutoNoSetter, AccessModifiers.None, AccessModifiers.None, false, true);
    }

    private static void Write(TypeInfo type, string name, string description, PropertyKind kind, AccessModifiers accessModifier, AccessModifiers setterModifier, bool isStatic, bool isInterfaceMember, string backingField = null, string defaultValue = null)
    {
        var prop = new PropertyDeclaration
        {
            IsStatic = isStatic,
            IsInterfaceMember = isInterfaceMember,
            Name = name,
            AccessModifier = accessModifier,
            SetterModifier = setterModifier,
            Summary = !string.IsNullOrWhiteSpace(description) ? new List<string>(1) { description } : null,
            Kind = kind,
            ReturnType = type,
            BackingField = backingField,
            DefaultValue = defaultValue
        };
        CSharpFile.Property(prop);
    }
}

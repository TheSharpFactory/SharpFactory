namespace SF.Models;

/// <summary>
/// The characters a database, schema, object and column name may have (user, 2026-11-09: A-Z, a-z, 0-9, spaces and
/// '_'). The schema reader finds identity, computed and always generated columns with a name that embeds the object's
/// full name, so a '.' or another special character breaks it silently; such names are not supported by the app and
/// are reported instead.
/// </summary>
public static class NameRules
{
    /// <summary>
    /// True for the names of databases, tables, views, stored procedures, functions, sequences and columns: ASCII
    /// letters, digits, '_' and spaces.
    /// </summary>
    public static bool IsSupportedObjectName(string name)
    {
        if(string.IsNullOrWhiteSpace(name))
            return false;

        foreach(var c in name)
        {
            if(!IsAsciiLetterOrDigit(c) && c != '_' && c != ' ')
                return false;
        }

        return true;
    }

    /// <summary>
    /// True for column names: the same characters as <see cref="IsSupportedObjectName"/>.
    /// </summary>
    public static bool IsSupportedColumnName(string name)
    {
        return IsSupportedObjectName(name);
    }

    /// <summary>
    /// True for a C# identifier the generator writes: an ASCII letter or '_' first, then ASCII letters, digits and '_'.
    /// </summary>
    public static bool IsValidIdentifier(string name)
    {
        if(string.IsNullOrEmpty(name))
            return false;

        var first = name[0];
        if(!IsAsciiLetter(first) && first != '_')
            return false;

        foreach(var c in name)
        {
            if(!IsAsciiLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }

    /// <summary>
    /// True for schema names: an ASCII letter or '_' first, then ASCII letters, digits and '_' (no space and no leading
    /// digit: schema names are written as they are into namespaces and folders).
    /// </summary>
    public static bool IsSupportedSchemaName(string name)
    {
        if(string.IsNullOrWhiteSpace(name))
            return false;

        return IsValidIdentifier(name);
    }

    private static bool IsAsciiLetter(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }

    private static bool IsAsciiLetterOrDigit(char c)
    {
        return IsAsciiLetter(c) || (c >= '0' && c <= '9');
    }
}

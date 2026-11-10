#region Usings
using System.Collections.Generic;
#endregion

namespace SF.Models;

public static class Utils
{
    public static string GenerateEntityName(string storeName)
    {
        if(string.IsNullOrWhiteSpace(storeName))
            return null;

        var clean = CleanIdentifier(storeName);
        if(clean == null)
            return null;

        //a one-letter name such as "s" has nothing left once singularized: keep it as it is
        var singular = Inflector.Singularize(clean);
        if(string.IsNullOrEmpty(singular))
            singular = clean;

        return Inflector.Capitalize(singular);
    }
    public static string GeneratePropertyName(string propStoreName)
    {
        if(string.IsNullOrWhiteSpace(propStoreName))
        {
            if(propStoreName == null)
                return "NullName";

            return "BlankName_" + propStoreName.Length;
        }

        var clean = CleanIdentifier(propStoreName);
        if(clean == null)
            return null;

        return Inflector.Capitalize(clean);
    }

    private static readonly Dictionary<char, string> _invalidFirstChars = new Dictionary<char, string>(10)
    {
      { '_', "" },
      { '0', "Zero" },
      { '1', "One" },
      { '2', "Two" },
      { '3', "Three" },
      { '4', "Four" },
      { '5', "Five" },
      { '6', "Six" },
      { '7', "Seven" },
      { '8', "Eight" },
      { '9', "Nine" }
    };
    private static string CleanIdentifier(string storeName)
    {
        if(storeName[0] == '@')
            storeName = storeName.Substring(1);//for parameter names just remove the @ symbol from the first position

        var clean = storeName
            .Replace('-', '_')
            .Replace(' ', '_')
            .Replace('(', '_')
            .Replace(')', '_')
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace('-', '_')
            .Replace(".", "Dot")
            .Replace("+", "Plus")
            .Replace("#", "Number")
            .Replace("@", "At")
            .Replace("$", "Dollar")
            .Replace("%", "Percent")
            .Replace("&", "And")
            .Replace("*", "Asterisk")
            .Replace("|", "Or")
            .Replace("__", "_")
            .Replace("__", "_");//finally eliminate all consecutive lower dashes

        if(clean.Length == 0)
            return null;

        if(clean[clean.Length - 1] == '_')
            clean = clean.Remove(clean.Length - 1);

        if(clean.Length == 0)
            return null;

        if(_invalidFirstChars.ContainsKey(clean[0]))
            clean = _invalidFirstChars[clean[0]] + clean.Substring(1);

        if(!NameRules.IsValidIdentifier(clean))
            return null;

        return clean;
    }
}

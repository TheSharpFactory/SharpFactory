//based on https://github.com/abelepereira/Inflector-Portable
//Copyright (c) 2013 Scott Kirkland. MIT License: see THIRD-PARTY-NOTICES.md

#region Usings
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#endregion

namespace SF.Models;

public static class Inflector
{
    private static readonly List<Rule> _plurals = new List<Rule>(24);
    private static readonly List<Rule> _singulars = new List<Rule>(31);
    private static readonly List<string> _uncountables = new List<string>(0);//this is not good for generating unique names for entities.
    private static readonly List<string> _singularEndingInS = new List<string>(5);//this is to avoid converting words ending in s like Status to Statu when sigularizing

    static Inflector()
    {
        #region Default Rules
        AddPlural("$", "s");
        AddPlural("s$", "s");
        AddPlural("(ax|test)is$", "$1es");
        AddPlural("(octop|vir|alumn|fung)us$", "$1i");
        AddPlural("(alias|status)$", "$1es");
        AddPlural("(bu)s$", "$1ses");
        AddPlural("(buffal|tomat|volcan)o$", "$1oes");
        AddPlural("([ti])um$", "$1a");
        AddPlural("sis$", "ses");
        AddPlural("(?:([^f])fe|([lr])f)$", "$1$2ves");
        AddPlural("(hive)$", "$1s");
        AddPlural("([^aeiouy]|qu)y$", "$1ies");
        AddPlural("(x|ch|ss|sh)$", "$1es");
        AddPlural("(matr|vert|ind)ix|ex$", "$1ices");
        AddPlural("([m|l])ouse$", "$1ice");
        AddPlural("^(ox)$", "$1en");
        AddPlural("(quiz)$", "$1zes");

        AddSingular("s$", "");
        AddSingular("(n)ews$", "$1ews");
        AddSingular("([ti])a$", "$1um");
        AddSingular("((a)naly|(b)a|(d)iagno|(p)arenthe|(p)rogno|(s)ynop|(t)he)ses$", "$1$2sis");
        AddSingular("(^analy)ses$", "$1sis");
        AddSingular("([^f])ves$", "$1fe");
        AddSingular("(hive)s$", "$1");
        AddSingular("(tive)s$", "$1");
        AddSingular("([lr])ves$", "$1f");
        AddSingular("([^aeiouy]|qu)ies$", "$1y");
        AddSingular("(s)eries$", "$1eries");
        AddSingular("(m)ovies$", "$1ovie");
        AddSingular("(x|ch|ss|sh)es$", "$1");
        AddSingular("([m|l])ice$", "$1ouse");
        AddSingular("(bus)es$", "$1");
        AddSingular("(o)es$", "$1");
        AddSingular("(shoe)s$", "$1");
        AddSingular("(cris|ax|test)es$", "$1is");
        AddSingular("(octop|vir|alumn|fung)i$", "$1us");
        AddSingular("(alias|status)es$", "$1");
        AddSingular("^(ox)en", "$1");
        AddSingular("(vert|ind)ices$", "$1ex");
        AddSingular("(matr)ices$", "$1ix");
        AddSingular("(quiz)zes$", "$1");

        AddIrregular("tax", "taxes");
        AddIrregular("person", "people");
        AddIrregular("man", "men");
        AddIrregular("child", "children");
        AddIrregular("sex", "sexes");
        AddIrregular("move", "moves");
        AddIrregular("goose", "geese");
        AddIrregular("alumna", "alumnae");

        //AddUncountable("equipment");
        //AddUncountable("information");
        //AddUncountable("rice");
        //AddUncountable("money");
        //AddUncountable("species");
        //AddUncountable("series");
        //AddUncountable("fish");
        //AddUncountable("sheep");
        //AddUncountable("deer");
        //AddUncountable("aircraft");

        AddSingularEndingInS("address");
        AddSingularEndingInS("status");
        AddSingularEndingInS("alias");
        AddSingularEndingInS("basis");
        AddSingularEndingInS("genius");
        #endregion
    }

    public static string Pluralize(string word)
    {
        return ApplyRules(_plurals, word);
    }
    public static string Singularize(string word)
    {
        var count = _singularEndingInS.Count;
        var wordLower = word.ToLower();

        for(var i = 0; i < count; i++)
        {
            if(wordLower.EndsWith(_singularEndingInS[i], StringComparison.Ordinal))
                return word;
        }

        return ApplyRules(_singulars, word);
    }

    public static string Titleize(string word)
    {
        return Regex.Replace(Humanize(Underscore(word)), @"\b([a-z])", GetFirstCapture);
    }

    private static string GetFirstCapture(Match match)
    {
        return match.Captures[0].Value.ToUpper();
    }

    public static string Humanize(string lowercaseAndUnderscoredWord)
    {
        return Capitalize(Regex.Replace(lowercaseAndUnderscoredWord, @"_", " "));
    }
    public static string Pascalize(string lowercaseAndUnderscoredWord)
    {
        return Regex.Replace(lowercaseAndUnderscoredWord, "(?:^|_)(.)", GetSecondGroup);
    }
    private static string GetSecondGroup(Match match)
    {
        return match.Groups[1].Value.ToUpper();
    }
    public static string Camelize(string lowercaseAndUnderscoredWord)
    {
        return Uncapitalize(Pascalize(lowercaseAndUnderscoredWord));
    }
    public static string Underscore(string pascalCasedWord)
    {
        return Regex.Replace(Regex.Replace(Regex.Replace(pascalCasedWord, @"([A-Z]+)([A-Z][a-z])", "$1_$2"), @"([a-z\d])([A-Z])", "$1_$2"), @"[-\s]", "_").ToLower();
    }
    public static string Capitalize(string word)
    {
        if(char.IsLower(word[0]))
            return char.ToUpperInvariant(word[0]).ToString() + word.Substring(1);

        return word;
    }
    public static string Uncapitalize(string word)
    {
        if(char.IsUpper(word[0]))
            return char.ToLowerInvariant(word[0]).ToString() + word.Substring(1);

        return word;
    }
    public static string Ordinalize(string numberString)
    {
        return Ordanize(int.Parse(numberString), numberString);
    }
    public static string Ordinalize(int number)
    {
        return Ordanize(number, number.ToString());
    }
    public static string Dasherize(string underscoredWord)
    {
        return underscoredWord.Replace('_', '-');
    }

    private static void AddIrregular(string singular, string plural)
    {
        AddPlural("(" + singular[0] + ")" + singular.Substring(1) + "$", "$1" + plural.Substring(1));
        AddSingular("(" + plural[0] + ")" + plural.Substring(1) + "$", "$1" + singular.Substring(1));
    }
    internal static void AddUncountable(string word)
    {
        _uncountables.Add(word.ToLower());
    }
    private static void AddSingularEndingInS(string word)
    {
        _singularEndingInS.Add(word.ToLower());
    }
    private static void AddPlural(string rule, string replacement)
    {
        _plurals.Add(new Rule(rule, replacement));
    }
    private static void AddSingular(string rule, string replacement)
    {
        _singulars.Add(new Rule(rule, replacement));
    }

    private static string ApplyRules(List<Rule> rules, string word)
    {
        var count = _uncountables.Count;
        var wordLower = word.ToLower();

        for(var i = 0; i < count; i++)
        {
            if(wordLower.EndsWith(_uncountables[i], StringComparison.Ordinal))
                return word;
        }

        var result = word;
        count = rules.Count - 1;

        for(var i = count; i >= 0; i--)
        {
            if((result = rules[i].Apply(word)) != null)
                return result;
        }

        return word;
    }
    private static string Ordanize(int number, string numberString)
    {
        var nMod100 = number % 100;

        if(nMod100 >= 11 && nMod100 <= 13)
        {
            return numberString + "th";
        }

        switch(number % 10)
        {
            case 1:
                return numberString + "st";
            case 2:
                return numberString + "nd";
            case 3:
                return numberString + "rd";
            default:
                return numberString + "th";
        }
    }
    private sealed class Rule
    {
        private readonly Regex _regex;
        private readonly string _replacement;

        public Rule(string pattern, string replacement)
        {
            _regex = new Regex(pattern, RegexOptions.IgnoreCase);
            _replacement = replacement;
        }

        public string Apply(string word)
        {
            if(!_regex.IsMatch(word))
                return null;

            return _regex.Replace(word, _replacement);
        }
    }
}

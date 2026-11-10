#region Usings
using System;
using System.Collections.Generic;
#endregion

namespace SF.Engine;

/// <summary>
/// Compares two lists of descriptions (what a model stores and what the database gives) for SchemaCheck.
/// </summary>
public static class ListComparison
{
    /// <summary>
    /// One line per difference: items only in the model, items only in the database (each extra copy of a repeated
    /// item counts), or "same items, different order". Empty when the lists are equal. Null counts as empty.
    /// </summary>
    public static List<string> Describe(string what, IEnumerable<string> stored, IEnumerable<string> fresh)
    {
        var s = stored == null ? new List<string>() : [.. stored];
        var f = fresh == null ? new List<string>() : [.. fresh];
        var result = new List<string>();

        if(AreEqual(s, f))
            return result;

        var onlyStored = Subtract(s, f);
        var onlyFresh = Subtract(f, s);

        if(onlyStored.Count == 0 && onlyFresh.Count == 0)
        {
            result.Add($"{what}: same items, different order");
            return result;
        }

        foreach(var item in onlyStored)
            result.Add($"{what}: only in the model    -> {item}");

        foreach(var item in onlyFresh)
            result.Add($"{what}: only in the database -> {item}");

        return result;
    }

    private static bool AreEqual(List<string> one, List<string> two)
    {
        if(one.Count != two.Count)
            return false;

        for(var i = 0; i < one.Count; i++)
        {
            if(!string.Equals(one[i], two[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    /// <summary>
    /// The items of <paramref name="from"/> left after taking away one copy for each copy in <paramref name="other"/>.
    /// </summary>
    private static List<string> Subtract(List<string> from, List<string> other)
    {
        var available = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach(var item in other)
        {
            available.TryGetValue(item, out var count);
            available[item] = count + 1;
        }

        var result = new List<string>();
        foreach(var item in from)
        {
            if(available.TryGetValue(item, out var count) && count > 0)
                available[item] = count - 1;
            else
                result.Add(item);
        }

        return result;
    }
}

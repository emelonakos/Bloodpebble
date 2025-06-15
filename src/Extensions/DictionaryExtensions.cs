
using System.Collections.Generic;

namespace Bloodpebble.Extensions;

public static class DictionaryExtensions
{
    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
    where TValue : new()
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        if (!dict.TryGetValue(key, out TValue val))
        {
            val = new TValue();
            dict.Add(key, val);
        }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        return val;
    }
}


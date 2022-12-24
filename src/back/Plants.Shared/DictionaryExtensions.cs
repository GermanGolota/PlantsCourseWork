﻿namespace Plants.Shared;

public static class DictionaryExtensions
{
    public static void AddList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value)
    {
        if (dict.ContainsKey(key))
        {
            dict[key].Add(value);
        }
        else
        {
            dict[key] = new() { value };
        }
    }

    public static void AddList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, IEnumerable<TValue> values)
    {
        if (dict.ContainsKey(key))
        {
            dict[key].AddRange(values);
        }
        else
        {
            dict[key] = values.ToList();
        }
    }
}

using System.Collections.Generic;

namespace beehivekiln.util
{
    public static class DictionaryExtensions
    {
        public static bool ChangeKeyInDict<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey oldKey, TKey newKey)
        {
            if (!dict.Remove(oldKey, out TValue value))
                return false;

            dict[newKey] = value;
            return true;
        }
    }
}
namespace ChickenBot.Core.Models
{
    public static class INIExtensions
    {
        public static bool Bool<K>(this IDictionary<K, string> dict, K key, bool defaultValue)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                return defaultValue;
            }

            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return defaultValue;
        }
    }
}

using System.Diagnostics.CodeAnalysis;

namespace ChickenBot.AdminCommands
{
    public static class Extensions
    {
        /// <summary>
        /// Adds to a list in the specified dictionary, at the specified key. Creates a new list if one does not exist.
        /// </summary>
        /// <typeparam name="K">Key type</typeparam>
        /// <typeparam name="T">Value type</typeparam>
        /// <typeparam name="L">List type</typeparam>
        /// <param name="dictionary">Dictionary to insert value into</param>
        /// <param name="key">The key of the list to set or append to</param>
        /// <param name="value">The value to insert into the list</param>
        public static void AddOrAppend<K, T, L>(this IDictionary<K, L> dictionary, K key, T value) where T : class where L : IList<T>, new()
        {
            if (dictionary.TryGetValue(key, out var existing))
            {
                existing.Add(value);
                return;
            }

            var list = new L();
            list.Add(value);
            dictionary.Add(key, list);
        }

        /// <summary>
        /// Gets a list of values from the dictionary, potentially expanding a single line entry into multiple, splitting by command and space.
        /// </summary>
        /// <typeparam name="K">The type of the key</typeparam>
        /// <typeparam name="L">The type of the list of string</typeparam>
        /// <param name="dictionary">Dictionary to read from</param>
        /// <param name="key">The key of the values to read</param>
        /// <param name="values">Resulting values, not null when this method returns true</param>
        /// <returns>true if the values were set, and at least one is present</returns>
        public static bool GetOrExpand<K, L>(this IReadOnlyDictionary<K, L> dictionary, K key, [NotNullWhen(true)] out L? values) where L : IList<string>, new()
        {
            if (!dictionary.TryGetValue(key, out var list))
            {
                values = default;
                return false;
            }

            if (list.Count == 0)
            {
                values = default;
                return false;
            }

            if (list.Count > 1)
            {
                values = list;
                return true;
            }

            var first = list[0];

            if (string.IsNullOrWhiteSpace(first))
            {
                values = list;
                return true;
            }

            var split = first.Split(',', ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (split.Length == 1)
            {
                values = list;
                return true;
            }

            values = new L();

            foreach (var value in split)
            {
                values.Add(value);
            }

            return true;
        }
    }
}

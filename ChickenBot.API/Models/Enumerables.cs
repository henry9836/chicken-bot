namespace ChickenBot.API.Models
{
    public static class Enumerables
    {
        public static async IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> values, Func<T, bool> predicate)
        {
            await foreach (var item in values)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        public static async IAsyncEnumerable<List<T>> Paginate<T>(this IAsyncEnumerable<T> values, int size)
        {
            var list = new List<T>(size);
            await foreach (var value in values)
            {
                list.Add(value);

                if (list.Count >= size)
                {
                    yield return list;
                    list = new List<T>(size);
                }
            }

            if (list.Count > 0)
            {
                yield return list;
            }
        }
    }
}

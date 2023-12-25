namespace Cooke.Gnissel;

public static class AsyncEnumerableExtensions
{
    public static async ValueTask<T[]> ToArrayAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default
    )
    {
        var listAsync = await source.ToListAsync(cancellationToken);
        return listAsync.ToArray();
    }

    public static async Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default
    )
    {
        var result = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            result.Add(item);
        }

        return result;
    }

    public static async Task<Dictionary<TKey, T>> ToDictionaryAsync<T, TKey>(
        this IAsyncEnumerable<T> source,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    )
        where TKey : notnull
    {
        var result = new Dictionary<TKey, T>();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            result.Add(keySelector(item), item);
        }

        return result;
    }

    public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<T, TKey, TElement>(
        this IAsyncEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector,
        CancellationToken cancellationToken = default
    )
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TElement>();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            result.Add(keySelector(item), elementSelector(item));
        }

        return result;
    }

    public static async ValueTask<T?> FirstOrDefaultAsync<T>(
        this IAsyncEnumerable<T> source,
        Predicate<T> predicate,
        CancellationToken cancellationToken = default
    )
    {
        await foreach (
            var element in source.WithCancellation(cancellationToken).ConfigureAwait(false)
        )
        {
            if (predicate(element))
            {
                return element;
            }
        }

        return default;
    }
}

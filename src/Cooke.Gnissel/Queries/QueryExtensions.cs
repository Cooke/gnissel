using System.Runtime.CompilerServices;

namespace Cooke.Gnissel.Queries;

public static class QueryExtensions
{
    public static async ValueTask<T[]> ToArrayAsync<T>(
        this IQuery<T> source,
        CancellationToken cancellationToken = default
    ) => (await source.ToListAsync(cancellationToken)).ToArray();

    public static async Task<List<T>> ToListAsync<T>(
        this IQuery<T> source,
        CancellationToken cancellationToken = default
    )
    {
        var result = new List<T>();
        await foreach (
            var item in source
                .ToAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)
        )
        {
            result.Add(item);
        }

        return result;
    }

    public static async Task<Dictionary<TKey, T>> ToDictionaryAsync<T, TKey>(
        this IQuery<T> source,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    )
        where TKey : notnull
    {
        var result = new Dictionary<TKey, T>();
        await foreach (
            var item in source
                .ToAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)
        )
        {
            result.Add(keySelector(item), item);
        }

        return result;
    }

    public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<T, TKey, TElement>(
        this IQuery<T> source,
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector,
        CancellationToken cancellationToken = default
    )
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TElement>();
        await foreach (
            var item in source
                .ToAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)
        )
        {
            result.Add(keySelector(item), elementSelector(item));
        }

        return result;
    }

    public static async Task<HashSet<T>> ToHashSetAsync<T>(
        this IQuery<T> source,
        CancellationToken cancellationToken = default
    ) => (await source.ToListAsync(cancellationToken)).ToHashSet();

    public static async IAsyncEnumerable<TResult> GroupBy<T1, T2, TKey, TElement, TResult>(
        this IAsyncEnumerable<(T1, T2)> source,
        Func<T1, T2, TKey> keySelector,
        Func<T1, T2, TElement> elementSelector,
        Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TKey : notnull
    {
        var groups = new Dictionary<TKey, List<(T1, T2)>>();
        var results = new List<(TKey key, List<(T1, T2)> list)>();
        await foreach (var element in source.WithCancellation(cancellationToken))
        {
            var key = keySelector(element.Item1, element.Item2);
            if (groups.TryGetValue(key, out var list))
            {
                list.Add(element);
            }
            else
            {
                list = [element];
                groups.Add(key, list);
                results.Add((key, list));
            }
        }

        foreach (var result in results)
        {
            yield return resultSelector(
                result.key,
                result.list.Select(item => elementSelector(item.Item1, item.Item2))
            );
        }
    }
}

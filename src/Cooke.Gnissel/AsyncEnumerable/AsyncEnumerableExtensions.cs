using System.Runtime.CompilerServices;
using Cooke.Gnissel.Internals;

namespace Cooke.Gnissel.AsyncEnumerable;

public static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<TResult> GroupBy<T1, T2, T3, TKey, TElement, TResult>(
        this IAsyncEnumerable<(T1, T2, T3)> source,
        Func<T1, T2, T3, TKey> groupBySelector,
        Func<T1, T2, T3, TElement> elementSelector,
        Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
        IEqualityComparer<TKey>? groupByComparer = default,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TKey : notnull
    {
        var groups = new Dictionary<TKey, List<(T1, T2, T3)>>(groupByComparer);
        var results = new List<(TKey key, List<(T1, T2, T3)> list)>();
        await foreach (var element in source.WithCancellation(cancellationToken))
        {
            var key = groupBySelector(element.Item1, element.Item2, element.Item3);
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
                result.list.Select(item => elementSelector(item.Item1, item.Item2, item.Item3))
            );
        }
    }

    public static IAsyncEnumerable<TResult> GroupBy<T1, T2, T3, TKey, TResult>(
        this IAsyncEnumerable<(T1, T2, T3)> source,
        Func<T1, T2, T3, TKey> groupBySelector,
        Func<TKey, IEnumerable<(T1, T2, T3)>, TResult> resultSelector,
        IEqualityComparer<TKey>? groupByComparer = default,
        CancellationToken cancellationToken = default
    )
        where TKey : notnull =>
        source.GroupBy(
            groupBySelector,
            (t1, t2, t3) => (t1, t2, t3),
            resultSelector,
            groupByComparer,
            cancellationToken
        );

    public static IAsyncEnumerable<TResult> GroupBy<T1, T2, T3, TKey, TInnerKey, TElement, TResult>(
        this IAsyncEnumerable<(T1, T2, T3)> source,
        Func<T1, T2, T3, TKey> groupBySelector,
        Func<T1, T2, T3, TElement> elementSelector,
        Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
        Func<TKey, TInnerKey>? groupByKeySelector,
        CancellationToken cancellationToken = default
    )
        where TKey : notnull
    {
        return source.GroupBy(
            groupBySelector,
            elementSelector,
            resultSelector,
            groupByKeySelector?.Let(CreateEqualityComparer!),
            cancellationToken
        );
    }

    private static IEqualityComparer<T> CreateEqualityComparer<T, TKey>(Func<T?, TKey?> selector) =>
        EqualityComparer<T>.Create(
            (l, r) => Equals(selector(l), selector(r)),
            x => selector(x)?.GetHashCode() ?? 0
        );
}

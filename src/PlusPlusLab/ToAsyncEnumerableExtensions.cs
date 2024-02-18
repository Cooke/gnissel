using Cooke.Gnissel.Utils;
using PlusPlusLab.Querying;

namespace PlusPlusLab;

public static class ToAsyncEnumerableExtensions
{
    public static ValueTask<T[]> ToArrayAsync<T>(
        this IToAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default
    ) => source.ToAsyncEnumerable().ToArrayAsync(cancellationToken);
}
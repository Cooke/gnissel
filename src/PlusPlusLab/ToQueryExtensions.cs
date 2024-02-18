using Cooke.Gnissel.Utils;
using PlusPlusLab.Querying;

namespace PlusPlusLab;

public static class ToQueryExtensions
{
    public static ValueTask<T[]> ToArrayAsync<T>(
        this IToQuery<T> source,
        CancellationToken cancellationToken = default
    ) => source.ToQuery().ToArrayAsync(cancellationToken);
}
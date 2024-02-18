using Cooke.Gnissel.Utils;
using PlusPlusLab.Querying;

namespace PlusPlusLab;

public static class QueryExtensions
{
    public static ValueTask<T[]> ToArrayAsync<T>(
        this IToQuery<T> source,
        CancellationToken cancellationToken = default
    ) => source.ToQuery().ToArrayAsync(cancellationToken);
}

using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Typed.Queries;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed;

public static class QueryExtensions
{
    public static ValueTask<T[]> ToArrayAsync<T>(
        this IToQuery<T> source,
        CancellationToken cancellationToken = default
    ) => source.ToQuery().ToArrayAsync(cancellationToken);
}

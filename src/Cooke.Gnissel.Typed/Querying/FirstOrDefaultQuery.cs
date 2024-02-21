using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Querying;

public class FirstOrDefaultQuery<T>(ExpressionQuery expressionQuery)
{
    public ValueTaskAwaiter<T?> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async ValueTask<T?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var query = (expressionQuery with { Limit = 1 }).ToQuery<T?>();

        await foreach (var value in query.WithCancellation(cancellationToken))
        {
            return value;
        }

        return default;
    }
}

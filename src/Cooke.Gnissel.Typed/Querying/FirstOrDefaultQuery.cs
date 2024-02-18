using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Querying;

public class FirstOrDefaultQuery<T>(DbOptionsTyped options, ExpressionQuery expressionQuery)
{
    public ValueTaskAwaiter<T?> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async ValueTask<T?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var query = new Query<T>(
            options.DbAdapter.RenderSql(
                options.SqlGenerator.Generate(expressionQuery with { Limit = 1 })
            ),
            options.ObjectReaderProvider.GetReaderFunc<T>(),
            options.DbConnector
        );

        await foreach (var value in query.WithCancellation(cancellationToken))
        {
            return value;
        }

        return default;
    }
}
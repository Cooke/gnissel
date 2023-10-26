using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public interface IQueryExecutor
{
    IAsyncEnumerable<TOut> Execute<TOut>(
        FormattedSql formattedSql,
        Func<RowReader, TOut> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        CancellationToken cancellationToken
    );
}

#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel;

public interface IQueryExecutor
{
    IAsyncEnumerable<TOut> Execute<TOut>(
        FormattedSql formattedSql,
        Func<DbDataReader, TOut> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        CancellationToken cancellationToken
    );
}

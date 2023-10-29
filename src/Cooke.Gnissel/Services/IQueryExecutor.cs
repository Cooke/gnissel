#region

using System.Data.Common;
using Cooke.Gnissel.CommandFactories;

#endregion

namespace Cooke.Gnissel.Services;

public interface IQueryExecutor
{
    IAsyncEnumerable<TOut> Execute<TOut>(
        Sql sql,
        Func<DbDataReader, TOut> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        CancellationToken cancellationToken
    );
}

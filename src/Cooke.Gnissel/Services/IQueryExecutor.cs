#region

using System.Data.Common;
using Cooke.Gnissel.CommandFactories;

#endregion

namespace Cooke.Gnissel.Services;

public interface IQueryExecutor
{
    IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        Func<DbDataReader, TOut> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        CancellationToken cancellationToken
    );
    
    ValueTask<int> Execute(Sql sql, ICommandFactory commandFactory, IDbAdapter dbAdapter, CancellationToken cancellationToken);
}

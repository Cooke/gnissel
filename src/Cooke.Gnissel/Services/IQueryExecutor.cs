#region

using System.Data.Common;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Statements;

#endregion

namespace Cooke.Gnissel.Services;

public interface IQueryExecutor
{
    IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        CancellationToken cancellationToken
    );
    
    IExecuteStatement Execute(Sql sql, ICommandFactory commandFactory, IDbAdapter dbAdapter, CancellationToken cancellationToken);
}

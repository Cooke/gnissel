#region

using System.Data.Common;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Statements;

#endregion

namespace Cooke.Gnissel.Services;

public interface IQueryExecutor
{
    IAsyncEnumerable<TOut> Query<TOut>(
        CompiledSql compiledSql,
        Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> mapper,
        IDbAccessFactory dbAccessFactory,
        CancellationToken cancellationToken
    );
}

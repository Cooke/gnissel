#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Queries;

public class SingleOrDefaultQuery<TOut> : IQuery
{
    private readonly Query<TOut> _innerQuery;

    public SingleOrDefaultQuery(
        RenderedSql renderedSql,
        Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> rowReader,
        IDbConnector dbConnector
    )
    {
        _innerQuery = new(renderedSql, rowReader, dbConnector);
    }

    public SingleOrDefaultQuery(Query<TOut> innerQuery)
    {
        _innerQuery = innerQuery;
    }

    public RenderedSql RenderedSql => _innerQuery.RenderedSql;

    public async ValueTask<TOut?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var enumerator = _innerQuery.GetAsyncEnumerator(cancellationToken);
        if (!await enumerator.MoveNextAsync())
        {
            return default;
        }

        var result = enumerator.Current;
        if (await enumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("Sequence contains more than one element");
        }

        return result;
    }

    public ValueTaskAwaiter<TOut?> GetAwaiter() => ExecuteAsync().GetAwaiter();
}

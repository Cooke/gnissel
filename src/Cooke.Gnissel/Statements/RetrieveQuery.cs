#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;

#endregion

namespace Cooke.Gnissel.Statements;

public class RetrieveQuery<TOut> : IAsyncEnumerable<TOut>
{
    private readonly RenderedSql _renderedSql;
    private readonly Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> _rowReader;
    private readonly IDbConnector _dbConnector;

    public RetrieveQuery(
        RenderedSql renderedSql,
        Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> rowReader,
        IDbConnector dbConnector
    )
    {
        _renderedSql = renderedSql;
        _rowReader = rowReader;
        _dbConnector = dbConnector;
    }

    public async IAsyncEnumerable<TOut> ExecuteAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var cmd = _dbConnector.CreateCommand();
        cmd.CommandText = _renderedSql.CommandText;
        cmd.Parameters.AddRange(_renderedSql.Parameters);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);
        await foreach (var value in _rowReader(reader, cancellationToken))
        {
            yield return value;
        }
    }

    public IAsyncEnumerator<TOut> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    ) => ExecuteAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
}

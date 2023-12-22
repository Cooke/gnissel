using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Statements;

public class ExecuteQuery
{
    private readonly IDbConnector _dbConnector;
    private readonly CancellationToken _cancellationToken;
    private readonly RenderedSql _renderedSql;

    public ExecuteQuery(
        IDbConnector dbConnector,
        RenderedSql renderedSql,
        CancellationToken cancellationToken
    )
    {
        _renderedSql = renderedSql;
        _dbConnector = dbConnector;
        _cancellationToken = cancellationToken;
    }

    public RenderedSql RenderedSql => _renderedSql;

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async ValueTask<int> ExecuteAsync()
    {
        await using var cmd = _dbConnector.CreateCommand();
        cmd.CommandText = _renderedSql.CommandText;
        cmd.Parameters.AddRange(_renderedSql.Parameters);
        return await cmd.ExecuteNonQueryAsync(_cancellationToken);
    }
}

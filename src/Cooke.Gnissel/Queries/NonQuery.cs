using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Queries;

public class NonQuery(
    IDbConnector dbConnector,
    RenderedSql renderedSql,
    CancellationToken cancellationToken
) : IQuery
{
    public RenderedSql RenderedSql => renderedSql;

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async ValueTask<int> ExecuteAsync()
    {
        await using var cmd = dbConnector.CreateCommand();
        cmd.CommandText = renderedSql.CommandText;
        cmd.Parameters.AddRange(renderedSql.Parameters);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

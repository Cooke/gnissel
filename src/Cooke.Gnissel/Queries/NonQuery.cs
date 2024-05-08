using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Queries;

public class NonQuery(IDbConnector dbConnector, RenderedSql renderedSql) : INonQuery
{
    public RenderedSql RenderedSql => renderedSql;

    public ValueTaskAwaiter<int> GetAwaiter() => ExecuteAsync().GetAwaiter();

    public async ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var cmd = dbConnector.CreateCommand();
        cmd.CommandText = renderedSql.CommandText;
        cmd.Parameters.AddRange(renderedSql.Parameters);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public class QueryExecutor : IQueryExecutor
{
    public async IAsyncEnumerable<TOut> Execute<TOut>(
        FormattedSql formattedSql,
        Func<RowReader, TOut> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var cmd = commandFactory.CreateCommand();
        cmd.CommandText = formattedSql.Sql;
        foreach (var parameter in formattedSql.Parameters.Select(dbAdapter.CreateParameter))
        {
            cmd.Parameters.Add(parameter);
        }

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return mapper(new RowReader(reader));
        }
    }
}

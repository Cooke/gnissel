using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public static class QueryExecutor
{
    internal static async IAsyncEnumerable<TOut> Execute<TOut>(
        FormattedSql formattedSql,
        Func<Row, TOut> mapper,
        ICommandProvider commandProvider,
        IDbAdapter dbAdapter,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var cmd = commandProvider.CreateCommand();
        cmd.CommandText = formattedSql.Sql;
        foreach (var parameter in formattedSql.Parameters.Select(dbAdapter.CreateParameter))
        {
            cmd.Parameters.Add(parameter);
        }

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return mapper(new Row(reader));
        }
    }
}
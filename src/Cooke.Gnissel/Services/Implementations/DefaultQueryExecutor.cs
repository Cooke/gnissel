#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultQueryExecutor : IQueryExecutor
{
    public async IAsyncEnumerable<TOut> Execute<TOut>(
        FormattedSql formattedSql,
        Func<DbDataReader, TOut> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var cmd = commandFactory.CreateCommand();
        cmd.CommandText = formattedSql.Sql;
        foreach (
            var parameter in formattedSql.Parameters.Select(
                (p) => dbAdapter.CreateParameter(p.value, p.dbType)
            )
        )
        {
            cmd.Parameters.Add(parameter);
        }

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return mapper(reader);
        }
    }
}

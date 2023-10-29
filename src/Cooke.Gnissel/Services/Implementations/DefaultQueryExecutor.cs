#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultQueryExecutor : IQueryExecutor
{
    public async IAsyncEnumerable<TOut> Execute<TOut>(
        Sql sql,
        Func<DbDataReader, TOut> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var cmd = commandFactory.CreateCommand();
        dbAdapter.PopulateCommand(cmd, sql);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return mapper(reader);
        }
    }
}

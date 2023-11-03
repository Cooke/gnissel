#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultQueryExecutor : IQueryExecutor
{
    public async IAsyncEnumerable<TOut> Query<TOut>(
        CompiledSql compiledSql,
        Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> mapper,
        IDbAccessFactory dbAccessFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var cmd = dbAccessFactory.CreateCommand();
        cmd.CommandText = compiledSql.CommandText;
        cmd.Parameters.AddRange(compiledSql.Parameters);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);
        await foreach (var value in mapper(reader, cancellationToken))
        {
            yield return value;
        }
    }
}

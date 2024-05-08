#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Queries;

public class SingleQuery<TOut>(
    RenderedSql renderedSql,
    Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> rowReader,
    IDbConnector dbConnector
) : IQuery
{
    public RenderedSql RenderedSql => renderedSql;

    public async ValueTask<TOut> ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var cmd = dbConnector.CreateCommand();
        cmd.CommandText = renderedSql.CommandText;
        cmd.Parameters.AddRange(renderedSql.Parameters);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);

        TOut result = default!;
        bool hasResult = false;
        await foreach (var item in rowReader(reader, cancellationToken))
        {
            if (hasResult)
            {
                throw new InvalidOperationException("Sequence contains more than one element");
            }

            result = item;
            hasResult = true;
        }

        if (!hasResult)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }

        return result;
    }
}

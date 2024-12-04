#region

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;

#endregion

namespace Cooke.Gnissel.Queries;

public class Query<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOut
>(
    RenderedSql renderedSql,
    Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> rowReader,
    IDbConnector dbConnector
) : IQuery<TOut>, IAsyncEnumerable<TOut>
{
    public RenderedSql RenderedSql => renderedSql;

    public async IAsyncEnumerable<TOut> ExecuteAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var cmd = dbConnector.CreateCommand();
        cmd.CommandText = renderedSql.CommandText;
        cmd.Parameters.AddRange(renderedSql.Parameters);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        await foreach (var value in rowReader(reader, cancellationToken))
        {
            yield return value;
        }
    }

    public IAsyncEnumerator<TOut> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    ) => ExecuteAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
}

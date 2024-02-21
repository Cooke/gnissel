using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Utils;

internal static class DbReaderUtils
{
    public static async IAsyncEnumerable<TOut> ReadRows<TOut>(
        this DbDataReader reader,
        ObjectReader<TOut> objectReader,
        [EnumeratorCancellation] CancellationToken innerCancellationToken
    )
    {
        while (await reader.ReadAsync(innerCancellationToken))
        {
            yield return objectReader.Read(reader, 0);
        }
    }

    public static async IAsyncEnumerable<TOut> ReadRows<TOut>(
        this DbDataReader reader,
        Func<DbDataReader, TOut> mapper,
        [EnumeratorCancellation] CancellationToken innerCancellationToken
    )
    {
        while (await reader.ReadAsync(innerCancellationToken))
        {
            yield return mapper(reader);
        }
    }
}

using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Utils;

internal static class ObjectReaderProviderExtensions
{
    public static Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> GetReaderFunc<TOut>(
        this IObjectReaderProvider provider,
        DbOptions dbOptions
    )
    {
        var objectReader = provider.Get<TOut>(dbOptions);
        return (reader, cancellationToken) => reader.ReadRows(objectReader, cancellationToken);
    }
}

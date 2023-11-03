#region

using System.Data.Common;
using System.Runtime.CompilerServices;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultRowReader : IRowReader
{
    private readonly IObjectReaderProvider _objectReaderProvider;

    public DefaultRowReader(IObjectReaderProvider objectReaderProvider)
    {
        _objectReaderProvider = objectReaderProvider;
    }

    public async IAsyncEnumerable<TOut> Read<TOut>(
        DbDataReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var objectReader = _objectReaderProvider.GetReader<TOut>();
        // var ordinalCache = stackalloc  int[objectReader.Width];
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return objectReader.Read(reader, 0);
        }
    }
}

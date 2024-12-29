using System.Buffers;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Cooke.Gnissel.Internals;

internal static class DbReaderUtils
{
    public static async IAsyncEnumerable<TOut> ReadRows<TOut>(
        this DbDataReader reader,
        ObjectReader<TOut> objectReader,
        [EnumeratorCancellation] CancellationToken innerCancellationToken
    )
    {
        var ordinalsByReadIndex = ArrayPool<int>.Shared.Rent(objectReader.ReadDescriptors.Length);
        var ordinalReader = new OrdinalReader(ordinalsByReadIndex);

        try
        {
            PopulateOrdinals(reader, objectReader, ordinalsByReadIndex);

            while (await reader.ReadAsync(innerCancellationToken))
            {
                yield return objectReader.Read(reader, ordinalReader);
                ordinalReader.Reset();
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(ordinalsByReadIndex);
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

    private static void PopulateOrdinals<TOut>(
        DbDataReader reader,
        ObjectReader<TOut> objectReader,
        int[] ordinalsByReadIndex
    )
    {
        Span<bool> consumedOrdinals = stackalloc bool[reader.FieldCount];
        for (var readIndex = 0; readIndex < objectReader.ReadDescriptors.Length; readIndex++)
        {
            var readDescriptor = objectReader.ReadDescriptors[readIndex];
            if (readDescriptor is NameReadDescriptor { Name: var ordinalName })
            {
                var foundOrdinal = false;
                for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                {
                    if (
                        !consumedOrdinals[ordinal]
                        && reader
                            .GetName(ordinal)
                            .Equals(ordinalName, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        ordinalsByReadIndex[readIndex] = ordinal;
                        consumedOrdinals[ordinal] = true;
                        foundOrdinal = true;
                        break;
                    }
                }

                if (!foundOrdinal)
                {
                    throw new InvalidOperationException(
                        "Column not found or already mapped: " + ordinalName
                    );
                }
            }
            else if (readDescriptor is NextOrdinalReadDescriptor)
            {
                ordinalsByReadIndex[readIndex] = readIndex;
            }
            else
            {
                throw new InvalidOperationException("Unknown read descriptor type");
            }
        }
    }
}

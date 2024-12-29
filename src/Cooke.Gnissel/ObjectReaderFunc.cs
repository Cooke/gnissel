using System.Data.Common;

namespace Cooke.Gnissel;

public delegate TOut ObjectReaderFunc<out TOut>(DbDataReader dataReader, Ordinals ordinals);

public readonly struct Ordinals(int[] innerOrdinals, int offset, int length)
{
    private readonly int _offset = offset;

    public int this[int index] => innerOrdinals[_offset + index];

    public int Length => length;

    public Ordinals Slice(int offset, int slicedLength)
    {
        if (slicedLength > length - offset)
        {
            throw new ArgumentOutOfRangeException(nameof(slicedLength));
        }

        return new Ordinals(innerOrdinals, _offset + offset, slicedLength);
    }
}

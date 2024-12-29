using System.Data.Common;

namespace Cooke.Gnissel;

public delegate TOut ObjectReaderFunc<out TOut>(
    DbDataReader dataReader,
    OrdinalReader ordinalReader
);

// public readonly struct OrdinalReader(int[] innerOrdinals, int offset, int length)
// {
//     private readonly int _offset = offset;
//
//     public int this[int index] => innerOrdinals[_offset + index];
//
//     public int Length => length;
//
//     public OrdinalReader Slice(int offset, int slicedLength)
//     {
//         if (slicedLength > length - offset)
//         {
//             throw new ArgumentOutOfRangeException(nameof(slicedLength));
//         }
//
//         return new OrdinalReader(innerOrdinals, _offset + offset, slicedLength);
//     }
// }

public class OrdinalReader(int[] ordinals)
{
    private int _index = 0;

    public int Read() => ordinals[_index++];

    public void Reset() => _index = 0;

    public SnapshotOrdinalReader CreateSnapshot() => new(ordinals, _index);
}

public struct SnapshotOrdinalReader(int[] ordinals, int index)
{
    private int _index = index;

    public int Read() => ordinals[_index++];
}

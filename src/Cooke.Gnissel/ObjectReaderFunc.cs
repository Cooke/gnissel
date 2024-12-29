using System.Data.Common;

namespace Cooke.Gnissel;

public delegate TOut ObjectReaderFunc<out TOut>(
    DbDataReader dataReader,
    OrdinalReader ordinalReader
);

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

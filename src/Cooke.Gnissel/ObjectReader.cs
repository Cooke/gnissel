using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel;

public interface IObjectReader
{
    Type ObjectType { get; }

    ImmutableArray<ReadDescriptor> ReadDescriptors { get; }
}

public delegate TOut ObjectReaderFunc<out TOut>(
    DbDataReader dataReader,
    OrdinalReader ordinalReader
);

public class OrdinalReader(int[] ordinals)
{
    private int _index = 0;

    public int Read() => ordinals[_index++];

    public void Reset() => _index = 0;
}

public class ObjectReader<TOut>(
    ObjectReaderFunc<TOut> read,
    ImmutableArray<ReadDescriptor> readDescriptors
) : IObjectReader
{
    public ObjectReaderFunc<TOut> Read { get; } = read;

    public ImmutableArray<ReadDescriptor> ReadDescriptors { get; } = readDescriptors;

    public Type ObjectType => typeof(TOut);
}

public abstract record ReadDescriptor;

public record NextOrdinalReadDescriptor : ReadDescriptor;

public record NameReadDescriptor(string Name) : ReadDescriptor;

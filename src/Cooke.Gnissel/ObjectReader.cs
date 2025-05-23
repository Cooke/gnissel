using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel;

public interface IObjectReader
{
    public static IObjectReader Create<T>(
        ObjectReaderFunc<T> read,
        Func<ImmutableArray<ReadDescriptor>> readDescriptorsFunc
    ) => new ObjectReader<T>(read, readDescriptorsFunc);

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
    Func<ImmutableArray<ReadDescriptor>> readDescriptorsFunc
) : IObjectReader
{
    private ImmutableArray<ReadDescriptor>? _readDescriptors;

    public ObjectReaderFunc<TOut> Read { get; } = read;

    public ImmutableArray<ReadDescriptor> ReadDescriptors
    {
        get
        {
            _readDescriptors ??= readDescriptorsFunc();
            return _readDescriptors.Value;
        }
    }

    public Type ObjectType => typeof(TOut);
}

public abstract record ReadDescriptor
{
    public abstract ReadDescriptor WithParent(string parent);
}

public record NextOrdinalReadDescriptor : ReadDescriptor
{
    public override ReadDescriptor WithParent(string parent) => new NameReadDescriptor(parent);
}

public record NameReadDescriptor(string Name) : ReadDescriptor
{
    public override ReadDescriptor WithParent(string? parent) =>
        string.IsNullOrEmpty(parent) ? this : new NameReadDescriptor(Name);
}

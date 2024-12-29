using System.Collections.Immutable;

namespace Cooke.Gnissel;

public class ObjectReader<TOut>(
    ObjectReaderFunc<TOut> read,
    ImmutableArray<ReadDescriptor> readDescriptors
)
{
    public ObjectReaderFunc<TOut> Read { get; } = read;

    public ImmutableArray<ReadDescriptor> ReadDescriptors { get; } = readDescriptors;
}

public abstract record ReadDescriptor;

public record PositionReadDescriptor(int Position) : ReadDescriptor;

public record NameReadDescriptor(string Name) : ReadDescriptor;

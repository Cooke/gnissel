using System.Collections.Immutable;

namespace Cooke.Gnissel;

public interface IObjectReader
{
    int ReadDescriptorsCount { get; }
}

public class ObjectReader<TOut>(
    ObjectReaderFunc<TOut> read,
    ImmutableArray<ReadDescriptor> readDescriptors
) : IObjectReader
{
    public ObjectReaderFunc<TOut> Read { get; } = read;

    public ImmutableArray<ReadDescriptor> ReadDescriptors { get; } = readDescriptors;

    int IObjectReader.ReadDescriptorsCount => ReadDescriptors.Length;
}

public abstract record ReadDescriptor;

public record NextOrdinalReadDescriptor : ReadDescriptor;

public record NameReadDescriptor(string Name) : ReadDescriptor;

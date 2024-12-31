using System.Collections.Immutable;

namespace Cooke.Gnissel;

public interface IObjectReader
{
    Type ObjectType { get; }
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

public class Test
{
    public static ObjectReader<T> CreateNonNullReader<T>(ObjectReader<T?> nullableReader)
        where T : struct =>
        new(
            ((dataReader, ordinalReader) => nullableReader.Read(dataReader, ordinalReader)!.Value),
            nullableReader.ReadDescriptors
        );
}

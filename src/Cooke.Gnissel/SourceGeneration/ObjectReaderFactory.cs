using System.Collections.Immutable;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.SourceGeneration;

public abstract record ReaderMetadata;

public record NestedReaderMetadata(IObjectReader Inner) : ReaderMetadata;

public record NameReaderMetadata(string Name, ReaderMetadata Inner) : ReaderMetadata
{
    public NameReaderMetadata(string Name)
        : this(Name, new NextOrdinalReaderMetadata()) { }
}

public record NextOrdinalReaderMetadata : ReaderMetadata;

public record MultiReaderMetadata(ReaderMetadata[] Readers) : ReaderMetadata;

public static class ObjectReaderFactory
{
    public static ObjectReader<T> Create<T>(
        IDbAdapter adapter,
        ObjectReaderFunc<T> objectReaderFunc,
        ReaderMetadata path
    ) =>
        adapter.IsDbMapped(typeof(T))
            ? new ObjectReader<T>(
                (reader, ordinalReader) => reader.GetFieldValue<T>(ordinalReader.Read()),
                [new NextOrdinalReadDescriptor()]
            )
            : new ObjectReader<T>(objectReaderFunc, [.. GetReadDescriptors(adapter, [], path)]);

    private static IEnumerable<ReadDescriptor> GetReadDescriptors(
        IDbAdapter adapter,
        ImmutableArray<string> objectPath,
        ReaderMetadata metadata
    )
    {
        switch (metadata)
        {
            case NextOrdinalReaderMetadata:
                yield return objectPath.Length > 0
                    ? new NameReadDescriptor(adapter.ToColumnName(objectPath))
                    : new NextOrdinalReadDescriptor();
                break;

            case NameReaderMetadata { Name: var name, Inner: var inner }:
                foreach (
                    var readDescriptor in GetReadDescriptors(adapter, objectPath.Add(name), inner)
                )
                {
                    yield return readDescriptor;
                }
                break;

            case NestedReaderMetadata { Inner: var inner }:
                foreach (var innerDescriptor in inner.ReadDescriptors)
                {
                    if (objectPath.Length > 0)
                    {
                        yield return new NameReadDescriptor(
                            adapter.ToColumnName(
                                innerDescriptor switch
                                {
                                    NameReadDescriptor nameReadDescriptor => objectPath.Add(
                                        nameReadDescriptor.Name
                                    ),
                                    NextOrdinalReadDescriptor => objectPath,
                                }
                            )
                        );
                    }
                    else
                    {
                        yield return innerDescriptor;
                    }
                }
                break;

            case MultiReaderMetadata { Readers: var subReaders }:
                foreach (var subReader in subReaders)
                {
                    foreach (var segment in GetReadDescriptors(adapter, objectPath, subReader))
                    {
                        yield return segment;
                    }
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(metadata));
        }
    }
}

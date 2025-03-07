using System.Collections.Immutable;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel;

public class ObjectReaderCreateContext(
    IObjectReaderProvider readerProvider,
    IObjectReaderDescriptorProvider descriptorProvider,
    IDbAdapter adapter
)
{
    public IObjectReaderProvider ReaderProvider => readerProvider;

    public IObjectReaderDescriptorProvider DescriptorProvider => descriptorProvider;

    public IDbAdapter Adapter => adapter;
}

public interface IObjectReaderDescriptorProvider
{
    public IObjectReaderDescriptor Get(Type type);
}

public interface IObjectReaderDescriptor : IObjectMapperDescriptor
{
    public IObjectReader Create(ObjectReaderCreateContext context);

    Type ObjectType { get; }

    ObjectReaderMetadata Metadata { get; }
}

public class ObjectReaderDescriptor<TObject>(
    Func<ObjectReaderCreateContext, ObjectReaderFunc<TObject>> factory,
    ObjectReaderMetadata metadata
) : IObjectReaderDescriptor
{
    public Type ObjectType => typeof(TObject);

    public ObjectReaderMetadata Metadata => metadata;

    public IObjectReader Create(ObjectReaderCreateContext context) =>
        new ObjectReader<TObject>(
            factory(context),
            [.. metadata.CreateReadDescriptors(context, [])]
        );
}

public abstract record ObjectReaderMetadata
{
    public abstract IEnumerable<ReadDescriptor> CreateReadDescriptors(
        ObjectReaderCreateContext context,
        ImmutableArray<string> objectPath
    );
};

public record NestedObjectReaderMetadata(Type ForType) : ObjectReaderMetadata
{
    public override IEnumerable<ReadDescriptor> CreateReadDescriptors(
        ObjectReaderCreateContext context,
        ImmutableArray<string> objectPath
    ) =>
        context.DescriptorProvider.Get(ForType).Metadata.CreateReadDescriptors(context, objectPath);
}

public record NameObjectReaderMetadata(string Name, ObjectReaderMetadata Inner)
    : ObjectReaderMetadata
{
    public NameObjectReaderMetadata(string Name)
        : this(Name, new NextOrdinalObjectReaderMetadata()) { }

    public override IEnumerable<ReadDescriptor> CreateReadDescriptors(
        ObjectReaderCreateContext context,
        ImmutableArray<string> objectPath
    ) => Inner.CreateReadDescriptors(context, objectPath.Add(Name));
}

public record NextOrdinalObjectReaderMetadata : ObjectReaderMetadata
{
    public override IEnumerable<ReadDescriptor> CreateReadDescriptors(
        ObjectReaderCreateContext context,
        ImmutableArray<string> objectPath
    )
    {
        yield return objectPath.Length > 0
            ? new NameReadDescriptor(context.Adapter.ToColumnName(objectPath))
            : new NextOrdinalReadDescriptor();
    }
}

public record MultiObjectReaderMetadata(ObjectReaderMetadata[] Readers) : ObjectReaderMetadata
{
    public override IEnumerable<ReadDescriptor> CreateReadDescriptors(
        ObjectReaderCreateContext context,
        ImmutableArray<string> objectPath
    ) => Readers.SelectMany(subReader => subReader.CreateReadDescriptors(context, objectPath));
}

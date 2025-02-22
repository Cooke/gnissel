using System.Collections.Immutable;
using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;

public static partial class ObjectReaders
{
    public static IObjectReaderProvider CreateProvider(IDbAdapter adapter) =>
        new ObjectReaderProviderBuilder(Descriptors).Build(adapter);

    static ObjectReaders()
    {
        Descriptors = [UserReaderDescriptor];
    }

    public static readonly ImmutableArray<IObjectReaderDescriptor> Descriptors;

    private static readonly ObjectReaderMetadata UserReaderMetadata = new MultiObjectReaderMetadata(
        [
            new NameObjectReaderMetadata("Name"),
            new NameObjectReaderMetadata(
                "Address",
                new NestedObjectReaderMetadata(typeof(Address?))
            ),
        ]
    );

    private static readonly ObjectReaderDescriptor<User?> UserReaderDescriptor = new(
        UserReaderFactory,
        UserReaderMetadata
    );

    private static ObjectReaderFunc<User?> UserReaderFactory(ObjectReaderCreateContext context)
    {
        var addressReader = context.ReaderProvider.Get<Address?>();
        return (reader, ordinalReader) =>
        {
            var name = reader.GetStringOrNull(ordinalReader.Read());
            var address = addressReader.Read(reader, ordinalReader);

            if (name is null && address != null)
            {
                return null;
            }

            return new User(name ?? throw new InvalidOperationException(), address!.Value);
        };
    }
}

using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel;

public static partial class ObjectReaders
{
    private static IImmutableList<IObjectReaderDescriptor> CreateAnons()
    {
        var metadata0 = new NextOrdinalObjectReaderMetadata();
        var readFactory0 = (ObjectReaderCreateContext context) =>
            (DbDataReader dbReader, OrdinalReader ordinalReader) =>
            {
                var name = dbReader.GetStringOrNull(ordinalReader.Read());

                if (name is null)
                {
                    return null;
                }

                return new { Name = name };
            };

        var metadata1 = new MultiObjectReaderMetadata(
            [
                new NameObjectReaderMetadata("Name"),
                new NameObjectReaderMetadata(
                    "Address",
                    new NestedObjectReaderMetadata(typeof(Address?))
                ),
            ]
        );
        var readFactory1 = (ObjectReaderCreateContext context) =>
        {
            var addressReader = context.ReaderProvider.Get<Address?>();
            return (DbDataReader dbReader, OrdinalReader ordinalReader) =>
                new
                {
                    Name = dbReader.GetString(ordinalReader.Read()),
                    Address = addressReader.Read(dbReader, ordinalReader),
                };
        };

        return
        [
            CreateObjectReader(readFactory0, metadata0),
            CreateObjectReader(readFactory1, metadata1),
        ];
    }

    private static ObjectReaderDescriptor<T> CreateObjectReader<T>(
        Func<ObjectReaderCreateContext, Func<DbDataReader, OrdinalReader, T>> factory,
        ObjectReaderMetadata metadata
    ) =>
        new(
            context =>
            {
                var innerReader = factory(context);
                return (reader, ordinalReader) => innerReader(reader, ordinalReader);
            },
            metadata
        );
}

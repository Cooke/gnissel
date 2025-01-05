﻿// See https://aka.ms/new-console-template for more information


using System.Collections.Immutable;
using Cooke.Gnissel;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;

Console.WriteLine("Hello, {type}");

class User(string Name, Address Address);

struct Address;

partial class Program
{
    public static void Setup()
    {
        var adapter = new NpgsqlDbAdapter(null!);
        new DbContext(new DbOptions(adapter, new ObjectReaderProvider(adapter)));
    }

    [ObjectReaderProvider]
    private partial class ObjectReaderProvider;

    private partial class ObjectReaderProvider(IDbAdapter adapter) : IObjectReaderProvider
    {
        private readonly IObjectReaderProvider _innerProvider = CreateProvider(adapter);

        public static IObjectReaderProvider CreateProvider(IDbAdapter adapter) =>
            new ObjectReaderProviderBuilder(ObjectReaderDescriptors).Build(adapter);

        static ObjectReaderProvider()
        {
            ObjectReaderDescriptors = [UserReaderDescriptor];
        }

        private static readonly ImmutableArray<IObjectReaderDescriptor> ObjectReaderDescriptors;

        private static readonly ObjectReaderMetadata UserReaderMetadata =
            new MultiObjectReaderMetadata(
                [
                    new NameObjectReaderMetadata("Name"),
                    new NameObjectReaderMetadata(
                        "Address",
                        new NestedObjectReaderMetadata(typeof(Address?))
                    ),
                ]
            );

        private static readonly ObjectReaderDescriptor<User?> UserReaderDescriptor =
            new(UserReaderFactory, UserReaderMetadata);

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

        public ObjectReader<TOut> Get<TOut>() => _innerProvider.Get<TOut>();
    }
}

using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public abstract record ReaderMetadata;

    public record NestedReaderMetadata(string Name, ReaderMetadata Inner) : ReaderMetadata;

    public record NameReaderMetadata(string Name) : ReaderMetadata;

    public record NextOrdinalReaderMetadata : ReaderMetadata;

    public record MultiReaderMetadata(ReaderMetadata[] Readers) : ReaderMetadata;

    public partial class GeneratedObjectReaderProvider : IObjectReaderProvider
    {
        public GeneratedObjectReaderProvider(IDbAdapter adapter)
        {
            _tupleUserDeviceReader = CreateObjectReader(
                adapter,
                ReadTupleDeviceUser,
                ReadTupleDeviceUserMetadata
            );
            _deviceReader = CreateObjectReader(adapter, ReadDevice, ReadDeviceMetadata);
            _int32Reader = CreateObjectReader(adapter, ReadInt32, ReadInt32Paths);
            _nullableInt32Reader = CreateObjectReader(
                adapter,
                ReadNullableInt32,
                ReadNullableInt32Paths
            );
            _userIdReader = CreateObjectReader(adapter, ReadUserId, ReadUserIdMetadata);
            _addressReader = CreateObjectReader(adapter, ReadAddress, ReadAddressMetadata);
            _userReader = CreateObjectReader(adapter, ReadUser, ReadUserMetadata);
        }

        public ObjectReader<TOut> Get<TOut>(DbOptions dbOptions)
        {
            object objectReader = typeof(TOut) switch
            {
                { Name: "User" } => _userReader,
                { Name: "Device" } => _deviceReader,
                { Name: "Int32" } => _int32Reader,
                { Name: "Nullable", GenericTypeArguments: [{ Name: "Int32" }] } =>
                    _nullableInt32Reader,
                { Name: "Tuple", GenericTypeArguments: [{ Name: "User" }, { Name: "Device" }] } =>
                    _tupleUserDeviceReader,
                _ => throw new NotSupportedException(
                    "No reader found for type " + typeof(TOut).Name
                ),
            };
            return (ObjectReader<TOut>)objectReader;
        }

        private static ObjectReader<T> CreateObjectReader<T>(
            IDbAdapter adapter,
            ObjectReaderFunc<T> objectReaderFunc,
            ReaderMetadata path
        ) =>
            adapter.IsDbMapped(typeof(T))
                ? new ObjectReader<T>(
                    (reader, ordinalReader) => reader.GetFieldValue<T>(ordinalReader.Read()),
                    []
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

                case NameReaderMetadata { Name: var name }:
                    yield return new NameReadDescriptor(adapter.ToColumnName(objectPath.Add(name)));
                    break;

                case NestedReaderMetadata { Name: var name, Inner: var inner }:
                    foreach (
                        var segment in GetReadDescriptors(adapter, objectPath.Add(name), inner)
                    )
                    {
                        yield return segment;
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

        private static bool IsNull(
            DbDataReader reader,
            OrdinalReader ordinalReader,
            IObjectReader objectReader
        )
        {
            var snapshot = ordinalReader.CreateSnapshot();
            for (var i = 0; i < objectReader.ReadDescriptorsCount; i++)
            {
                if (!reader.IsDBNull(snapshot.Read()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

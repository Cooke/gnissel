using System.Collections.Immutable;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider : IObjectReaderProvider
    {
        private readonly ImmutableDictionary<Type, object> _objectReaders;

        public GeneratedObjectReaderProvider(IDbAdapter adapter)
        {
            var readers = ImmutableDictionary.CreateBuilder<Type, object>();

            _tupleUserDeviceReader = ObjectReaderFactory.Create(
                adapter,
                ReadTupleDeviceUser,
                ReadTupleDeviceUserMetadata
            );
            readers.Add(_tupleUserDeviceReader.ObjectType, _tupleUserDeviceReader);

            _deviceReader = ObjectReaderFactory.Create(adapter, ReadDevice, ReadDeviceMetadata);
            readers.Add(_deviceReader.ObjectType, _deviceReader);

            _int32Reader = ObjectReaderFactory.Create(adapter, ReadInt32, ReadInt32Metadata);
            readers.Add(_int32Reader.ObjectType, _int32Reader);

            _nullableInt32Reader = ObjectReaderFactory.Create(
                adapter,
                ReadNullableInt32,
                ReadNullableInt32Paths
            );
            readers.Add(_nullableInt32Reader.ObjectType, _nullableInt32Reader);

            _userIdReader = ObjectReaderFactory.Create(adapter, ReadUserId, ReadUserIdMetadata);
            readers.Add(_userIdReader.ObjectType, _userIdReader);

            _addressReader = ObjectReaderFactory.Create(adapter, ReadAddress, ReadAddressMetadata);
            readers.Add(_addressReader.ObjectType, _addressReader);

            _userReader = ObjectReaderFactory.Create(adapter, ReadUser, ReadUserMetadata);
            readers.Add(_userReader.ObjectType, _userReader);

            _objectReaders = readers.ToImmutable();
        }

        public ObjectReader<TOut> Get<TOut>(DbOptions dbOptions) =>
            _objectReaders.TryGetValue(typeof(TOut), out var reader)
                ? (ObjectReader<TOut>)reader
                : throw new InvalidOperationException(
                    "No reader found for type " + typeof(TOut).Name
                );
    }
}

using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider : IObjectReaderProvider
    {
        public GeneratedObjectReaderProvider(IDbAdapter adapter)
        {
            _tupleUserDeviceReader = ObjectReaderFactory.Create(
                adapter,
                ReadTupleDeviceUser,
                ReadTupleDeviceUserMetadata
            );
            _deviceReader = ObjectReaderFactory.Create(adapter, ReadDevice, ReadDeviceMetadata);
            _int32Reader = ObjectReaderFactory.Create(adapter, ReadInt32, ReadInt32Paths);
            _nullableInt32Reader = ObjectReaderFactory.Create(
                adapter,
                ReadNullableInt32,
                ReadNullableInt32Paths
            );
            _userIdReader = ObjectReaderFactory.Create(adapter, ReadUserId, ReadUserIdMetadata);
            _addressReader = ObjectReaderFactory.Create(adapter, ReadAddress, ReadAddressMetadata);
            _userReader = ObjectReaderFactory.Create(adapter, ReadUser, ReadUserMetadata);
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
    }
}

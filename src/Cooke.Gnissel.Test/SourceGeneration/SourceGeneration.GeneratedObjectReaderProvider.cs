using System.Collections.Immutable;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider(IDbAdapter adapter) : IObjectReaderProvider
    {
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
                { IsConstructedGenericType: true, GenericTypeArguments: [{ Name: "Int32" }] } =>
                    throw new Exception(),
                _ => throw new NotSupportedException(
                    "No reader found for type " + typeof(TOut).Name
                ),
            };
            return (ObjectReader<TOut>)objectReader;
        }

        private static ObjectReader<T> CreateObjectReader<T>(
            IDbAdapter adapter,
            ObjectReaderFunc<T> objectReaderFunc,
            ImmutableArray<PathSegment> columns
        ) =>
            adapter.IsDbMapped(typeof(T))
                ? new ObjectReader<T>((reader, _) => reader.GetFieldValue<T>(0), [])
                : new ObjectReader<T>(objectReaderFunc, [.. columns.Select(adapter.ToColumnName)]);
    }
}

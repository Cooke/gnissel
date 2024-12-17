using System.Collections.Immutable;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider(IDbAdapter adapter) : IObjectReaderProvider
    {
        public ObjectReader<TOut> Get<TOut>(DbOptions dbOptions)
        {
            object objectReader = typeof(TOut).Name switch
            {
                "User" => _userReader,
                "Device" => _deviceReader,
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

using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public abstract record ReaderDescriptor;

    public record NestedReaderDescriptor(string Name, ReaderDescriptor Inner) : ReaderDescriptor;

    public record NameReaderDescriptor(string Name) : ReaderDescriptor;

    public record PositionReaderDescriptor(int Position) : ReaderDescriptor;

    public record MultiReaderDescriptor(ReaderDescriptor[] Readers) : ReaderDescriptor;

    public record ThunkReaderDescriptor(Func<ReaderDescriptor> Func) : ReaderDescriptor;

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
                _ => throw new NotSupportedException(
                    "No reader found for type " + typeof(TOut).Name
                ),
            };
            return (ObjectReader<TOut>)objectReader;
        }

        private static ObjectReader<T> CreateObjectReader<T>(
            IDbAdapter adapter,
            ObjectReaderFunc<T> objectReaderFunc,
            ReaderDescriptor path
        ) =>
            adapter.IsDbMapped(typeof(T))
                ? new ObjectReader<T>((reader, _) => reader.GetFieldValue<T>(0), [])
                : new ObjectReader<T>(objectReaderFunc, [.. GetPathSegments(adapter, null, path)]);

        private static IEnumerable<ReadDescriptor> GetPathSegments(
            IDbAdapter adapter,
            PathSegment? parent,
            ReaderDescriptor path
        )
        {
            switch (path)
            {
                case PositionReaderDescriptor { Position: var position }:
                    yield return new PositionReadDescriptor(position);
                    break;

                case NameReaderDescriptor { Name: var name }:
                    yield return new NameReadDescriptor(
                        adapter.ToColumnName(
                            PathSegment.Combine(parent, new ParameterPathSegment(name))
                        )
                    );
                    break;

                case NestedReaderDescriptor { Name: var name, Inner: var inner }:
                    foreach (
                        var segment in GetPathSegments(
                            adapter,
                            PathSegment.Combine(parent, new PropertyPathSegment(name)),
                            inner
                        )
                    )
                    {
                        yield return segment;
                    }
                    break;

                case MultiReaderDescriptor { Readers: var subReaders }:
                    foreach (var subReader in subReaders)
                    {
                        foreach (var segment in GetPathSegments(adapter, parent, subReader))
                        {
                            yield return segment;
                        }
                    }
                    break;

                case ThunkReaderDescriptor { Func: var func }:
                    foreach (var segment in GetPathSegments(adapter, parent, func()))
                    {
                        yield return segment;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(path));
            }
        }

        private static bool IsAllDbNull(DbDataReader reader, Ordinals ordinals)
        {
            for (var i = 0; i < ordinals.Length; i++)
            {
                if (!reader.IsDBNull(ordinals[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

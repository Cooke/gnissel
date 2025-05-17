using System.Collections.Immutable;

namespace Cooke.Gnissel.Services.Implementations;

public class DictionaryObjectReaderProvider(IImmutableDictionary<Type, IObjectReader> readers)
    : IObjectReaderProvider
{
    public ObjectReader<TOut> Get<TOut>() => (ObjectReader<TOut>)Get(typeof(TOut));

    public IObjectReader Get(Type type) =>
        readers.TryGetValue(type, out var reader)
            ? reader
            : throw new InvalidOperationException($"No reader found for type {type}");

    public static IObjectReaderProvider From(IEnumerable<IObjectReader> objectReaders)
    {
        var builder = ImmutableDictionary.CreateBuilder<Type, IObjectReader>();

        foreach (var reader in objectReaders)
        {
            builder[reader.ObjectType] = reader;
        }

        return new DictionaryObjectReaderProvider(builder.ToImmutable());
    }
}

using System.Collections.Immutable;

namespace Cooke.Gnissel.Services.Implementations;

public class DictionaryObjectReaderProvider(IImmutableDictionary<Type, IObjectReader> readers)
    : IObjectReaderProvider
{
    public ObjectReader<TOut> Get<TOut>() =>
        readers.TryGetValue(typeof(TOut), out var reader)
            ? (ObjectReader<TOut>)reader
            : throw new InvalidOperationException($"No reader found for type {typeof(TOut)}");

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
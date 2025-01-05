using System.Collections.Immutable;

namespace Cooke.Gnissel.Services.Implementations;

internal class DictionaryObjectReaderProvider(IImmutableDictionary<Type, IObjectReader> readers)
    : IObjectReaderProvider
{
    public ObjectReader<TOut> Get<TOut>() =>
        readers.TryGetValue(typeof(TOut), out var reader)
            ? (ObjectReader<TOut>)reader
            : throw new InvalidOperationException($"No reader found for type {typeof(TOut)}");
}

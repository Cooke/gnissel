using System.Collections.Immutable;

namespace Cooke.Gnissel.Services.Implementations;

public class DictionaryObjectWriterProvider(IImmutableDictionary<Type, IObjectWriter> writers)
    : IObjectWriterProvider
{
    public ObjectWriter<T> Get<T>() => (ObjectWriter<T>)Get(typeof(T));

    public IObjectWriter Get(Type type) =>
        writers.TryGetValue(type, out var writer)
            ? writer
            : throw new InvalidOperationException("No writer found for type " + type.Name);

    public static DictionaryObjectWriterProvider From(IEnumerable<IObjectWriter> writers)
    {
        var builder = ImmutableDictionary.CreateBuilder<Type, IObjectWriter>();
        foreach (var writer in writers)
        {
            builder.Add(writer.ObjectType, writer);
        }

        return new DictionaryObjectWriterProvider(builder.ToImmutable());
    }
}

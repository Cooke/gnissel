using System.Collections.Immutable;

namespace Cooke.Gnissel.Services.Implementations;

public class DictionaryObjectWriterProvider(IImmutableDictionary<Type, IObjectWriter> writers)
    : IObjectWriterProvider
{
    public ObjectWriter<T> Get<T>() =>
        writers.TryGetValue(typeof(T), out var writer)
            ? (ObjectWriter<T>)writer
            : DefaultObjectWriter<T>.Instance;

    private static class DefaultObjectWriter<T>
    {
        public static readonly ObjectWriter<T> Instance = new(
            (value, parameterWriter) => parameterWriter.Write(value)
        );
    }

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
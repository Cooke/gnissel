using System.Collections.Immutable;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

namespace Cooke.Gnissel.SourceGeneration;

public interface IObjectWriterDescriptorProvider
{
    public IObjectWriterDescriptor Get(Type type);
}

public class ObjectWriterCreateContext(
    IObjectWriterProvider readerProvider,
    IObjectWriterDescriptorProvider descriptorProvider,
    IDbAdapter adapter
)
{
    public IObjectWriterProvider WriterProvider => readerProvider;

    public IObjectWriterDescriptorProvider DescriptorProvider => descriptorProvider;

    public IDbAdapter Adapter => adapter;
}

internal class DictionaryObjectWriterProvider(IImmutableDictionary<Type, IObjectWriter> writers)
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
}

public class ObjectWriterProviderBuilder
{
    public ObjectWriterProviderBuilder() { }

    public ObjectWriterProviderBuilder(IEnumerable<IObjectWriterDescriptor> descriptors)
    {
        _descriptors = descriptors.ToDictionary(x => x.ObjectType);
    }

    private readonly Dictionary<Type, IObjectWriterDescriptor> _descriptors = new();

    private readonly ImmutableDictionary<Type, IObjectWriter>.Builder _writers =
        ImmutableDictionary.CreateBuilder<Type, IObjectWriter>();

    public IObjectWriterProvider Build(IDbAdapter adapter)
    {
        var provider = new InnerProvider(_writers, _descriptors, adapter);
        var createContext = new ObjectWriterCreateContext(provider, provider, adapter);
        foreach (var (type, descriptor) in _descriptors)
        {
            _writers.TryAdd(type, descriptor.Create(createContext));
        }

        return new DictionaryObjectWriterProvider(_writers.ToImmutable());
    }

    public void TryAdd(IObjectWriterDescriptor descriptor) =>
        _descriptors.TryAdd(descriptor.ObjectType, descriptor);

    public void Set(IObjectWriterDescriptor descriptor) =>
        _descriptors[descriptor.ObjectType] = descriptor;

    private class InnerProvider(
        ImmutableDictionary<Type, IObjectWriter>.Builder readers,
        Dictionary<Type, IObjectWriterDescriptor> descriptors,
        IDbAdapter adapter
    ) : IObjectWriterProvider, IObjectWriterDescriptorProvider
    {
        ObjectWriter<T> IObjectWriterProvider.Get<T>()
        {
            var createContext = new ObjectWriterCreateContext(this, this, adapter);
            if (readers.TryGetValue(typeof(T), out var reader))
            {
                return (ObjectWriter<T>)reader;
            }

            if (descriptors.TryGetValue(typeof(T), out var descriptor))
            {
                var newWriter = descriptor.Create(createContext);
                readers.Add(typeof(T), newWriter);
                return (ObjectWriter<T>)newWriter;
            }

            throw new InvalidOperationException($"No reader found for type {typeof(T)}");
        }

        IObjectWriterDescriptor IObjectWriterDescriptorProvider.Get(Type type) => descriptors[type];
    }
}

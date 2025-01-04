using System.Collections.Immutable;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

namespace Cooke.Gnissel.SourceGeneration;

public class ObjectReaderProviderBuilder
{
    private readonly Dictionary<Type, IObjectReaderDescriptor> _descriptors = new();

    private readonly ImmutableDictionary<Type, IObjectReader>.Builder _readers =
        ImmutableDictionary.CreateBuilder<Type, IObjectReader>();

    public IObjectReaderProvider Build(IDbAdapter adapter)
    {
        var provider = new InnerProvider(_readers, _descriptors, adapter);
        var createContext = new ObjectReaderCreateContext(provider, provider, adapter);
        foreach (var (type, descriptor) in _descriptors)
        {
            _readers.TryAdd(type, descriptor.Create(createContext));
        }

        return new DictionaryObjectReaderProvider(_readers.ToImmutable());
    }

    public void TryAdd(IObjectReaderDescriptor descriptor) =>
        _descriptors.TryAdd(descriptor.ObjectType, descriptor);

    public void Set(IObjectReaderDescriptor descriptor) =>
        _descriptors[descriptor.ObjectType] = descriptor;

    private class InnerProvider(
        ImmutableDictionary<Type, IObjectReader>.Builder readers,
        Dictionary<Type, IObjectReaderDescriptor> descriptors,
        IDbAdapter adapter
    ) : IObjectReaderProvider, IObjectReaderDescriptorProvider
    {
        ObjectReader<T> IObjectReaderProvider.Get<T>()
        {
            var createContext = new ObjectReaderCreateContext(this, this, adapter);
            if (readers.TryGetValue(typeof(T), out var reader))
            {
                return (ObjectReader<T>)reader;
            }

            if (descriptors.TryGetValue(typeof(T), out var descriptor))
            {
                var newReader = descriptor.Create(createContext);
                readers.Add(typeof(T), newReader);
                return (ObjectReader<T>)newReader;
            }

            throw new InvalidOperationException($"No reader found for type {typeof(T)}");
        }

        IObjectReaderDescriptor IObjectReaderDescriptorProvider.Get(Type type) => descriptors[type];
    }
}

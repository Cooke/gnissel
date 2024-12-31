//HintName: GeneratedObjectReaderProvider.cs
namespace Gnissel.SourceGeneration;

using System.Data.Common;
using Cooke.Gnissel;
using Cooke.Gnissel.SourceGeneration;
using System.Collections.Immutable;
using Cooke.Gnissel.Services;

public partial class GeneratedObjectReaderProvider
{
    private readonly ImmutableDictionary<Type, object> _objectReaders;
    
    public GeneratedObjectReaderProvider(IDbAdapter adapter)
    {
        var readers = ImmutableDictionary.CreateBuilder<Type, object>();
        
        _int32Reader = ObjectReaderFactory.Create(adapter, ReadInt32, ReadInt32Metadata);
        readers.Add(_int32Reader.ObjectType, _int32Reader);
        
        _objectReaders = readers.ToImmutable();
    }
    
    public ObjectReader<TOut> Get<TOut>(DbOptions dbOptions) =>
        _objectReaders.TryGetValue(typeof(TOut), out var reader)
        ? (ObjectReader<TOut>)reader
        : throw new InvalidOperationException("No reader found for type " + typeof(TOut).Name);
    
}

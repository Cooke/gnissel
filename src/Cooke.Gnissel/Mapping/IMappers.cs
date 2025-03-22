using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Mapping;

public interface IMapperProvider
{
    IObjectReaderProvider ReaderProvider { get; }
    
    IObjectWriterProvider WriterProvider { get; }
}

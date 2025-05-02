using Cooke.Gnissel.Services;

namespace Cooke.Gnissel;

public interface IMapperProvider
{
    IObjectReaderProvider ReaderProvider { get; }

    IObjectWriterProvider WriterProvider { get; }

    IDbNameProvider NameProvider { get; }
}

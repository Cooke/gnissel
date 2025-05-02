using Cooke.Gnissel;
using Cooke.Gnissel.Services;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers(IDbNameProvider nameProvider) : IMapperProvider
{
    public IDbNameProvider NameProvider { get; } = nameProvider;

    public IObjectReaderProvider ReaderProvider { get; init; } = new DbReaders(nameProvider);

    public IObjectWriterProvider WriterProvider { get; init; } = new DbWriters(nameProvider);
}

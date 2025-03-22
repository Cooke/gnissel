using Cooke.Gnissel;
using Cooke.Gnissel.Mapping;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers : IMapperProvider
{
    public DbReaders Readers { get; init; } = new DbReaders();

    public IObjectReaderProvider ReaderProvider => Readers;

    internal partial class DbReaders : IObjectReaderProvider
    {
        private IObjectReaderProvider? _readerProvider;
        private readonly IEnumerable<IObjectReader> _additionalReaders;

        public DbReaders()
            : this([]) { }

        public DbReaders(IEnumerable<IObjectReader> additionalReaders)
        {
            _additionalReaders = additionalReaders;
            UserReader = new ObjectReader<User?>(ReadUser, CreateReadUserDescriptors);
            AddressReader = new ObjectReader<Address?>(ReadAddress, CreateReadAddressDescriptors);
        }

        private IEnumerable<IObjectReader> GetAllReaders() =>
            [UserReader, AddressReader, .. _additionalReaders];

        public ObjectReader<TOut> Get<TOut>()
        {
            _readerProvider ??= DictionaryObjectReaderProvider.From(GetAllReaders());
            return _readerProvider.Get<TOut>();
        }
    }
}

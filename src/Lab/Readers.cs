using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers : IMapperProvider
{
    internal partial class DbReaders : IObjectReaderProvider
    {
        private IObjectReaderProvider? _readerProvider;

        public DbReaders(IDbNameProvider nameProvider)
        {
            NameProvider = nameProvider;
            UserReader = new ObjectReader<User?>(ReadUser, CreateReadUserDescriptors);
            AddressReader = new ObjectReader<Address?>(ReadAddress, CreateReadAddressDescriptors);
        }

        private IDbNameProvider NameProvider { get; }

        public ObjectReader<string?> StringReader { get; }
        public ObjectReader<int> Int32Reader { get; }
        public ObjectReader<int?> Int32NullableReader { get; }
        public IObjectReader Anonymous1Reader { get; }

        private IEnumerable<IObjectReader> GetAllReaders() =>
            [UserReader, AddressReader, .. CreateAnonymousReaders()];

        public ObjectReader<TOut> Get<TOut>()
        {
            _readerProvider ??= DictionaryObjectReaderProvider.From(GetAllReaders());
            return _readerProvider.Get<TOut>();
        }
    }
}

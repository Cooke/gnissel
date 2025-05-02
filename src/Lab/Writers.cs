using Cooke.Gnissel;
using Cooke.Gnissel.Services.Implementations;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbWriters : IObjectWriterProvider
    {
        public IDbNameProvider NameProvider { get; init; }

        private DictionaryObjectWriterProvider? _provider;

        public DbWriters(IDbNameProvider nameProvider)
        {
            NameProvider = nameProvider;
            UserIdWriter = new ObjectWriter<UserId?>(WriteUserId, CreateWriteUserIdDescriptors);
            UserWriter = new ObjectWriter<User?>(WriteUser, CreateWriteUserDescriptors);
            UserTypeWriter = new ObjectWriter<UserType?>(
                WriteUserType,
                CreateWriteUserTypeDescriptors
            );
            NonNullableUserTypeWriter = new ObjectWriter<UserType>(
                WriteUserType,
                CreateWriteUserTypeDescriptors
            );
            AddressWriter = new ObjectWriter<Address?>(WriteAddress, CreateWriteAddressDescriptors);
            StringWriter = new ObjectWriter<string?>(WriteString, CreateWriteStringDescriptors);
        }

        private IEnumerable<IObjectWriter> GetAll() =>
            [UserIdWriter, UserWriter, UserTypeWriter, NonNullableUserTypeWriter];

        public ObjectWriter<T> Get<T>()
        {
            _provider ??= DictionaryObjectWriterProvider.From(GetAll());
            return _provider.Get<T>();
        }

        public IObjectWriter Get(Type type)
        {
            throw new NotImplementedException();
        }
    }
}

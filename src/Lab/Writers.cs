using Cooke.Gnissel;
using Cooke.Gnissel.Services.Implementations;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    public ObjectWriters Writers { get; init; } = new ObjectWriters();

    IObjectWriterProvider IMapperProvider.WriterProvider => Writers;

    internal partial class ObjectWriters : IObjectWriterProvider
    {
        private DictionaryObjectWriterProvider? _provider;

        public ObjectWriters()
        {
            UserIdWriter = new ObjectWriter<UserId?>(WriteUserId);
            UserWriter = new ObjectWriter<User?>(WriteUser);
            UserTypeWriter = new ObjectWriter<UserType?>(WriteUserType);
            NonNullableUserTypeWriter = new ObjectWriter<UserType>(WriteUserType);
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

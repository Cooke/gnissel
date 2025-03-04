using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal static partial class ObjectWriters
{
    public static Action<User, IParameterWriter> CreateWriteUser(
        IObjectWriterProvider objectWriterProvider
    )
    {
        var userTypeWriter = objectWriterProvider.Get<UserType>();
        var addressWriter = objectWriterProvider.Get<Address>();
        var userIdWriter = objectWriterProvider.Get<UserId>();

        return (user, parameterWriter) =>
        {
            userIdWriter.Write(user.Id, parameterWriter);
            parameterWriter.Write(user.Name);
            parameterWriter.Write(user.Address);
            addressWriter.Write(user.Address, parameterWriter);
            userTypeWriter.Write(user.Type, parameterWriter);
        };
    }
}

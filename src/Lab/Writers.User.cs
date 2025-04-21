using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbWriters
    {
        public ObjectWriter<User?> UserWriter { get; init; }

        public void WriteUser(User? user, IParameterWriter parameterWriter)
        {
            UserIdWriter.Write(user?.Id, parameterWriter);
            parameterWriter.Write(user?.Name);
            parameterWriter.Write(user?.Address);
            UserTypeWriter.Write(user?.Type, parameterWriter);
        }
    }
}

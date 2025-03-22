using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class ObjectWriters
    {
        public ObjectWriter<UserId?> UserIdWriter { get; init; }
        
        public void WriteUserId(UserId? value, IParameterWriter parameterWriter) =>
            parameterWriter.Write(value?.Value);
    }
}
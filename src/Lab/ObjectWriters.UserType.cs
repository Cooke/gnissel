using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal static partial class ObjectWriters
{
    public static Action<UserType, IParameterWriter> WriteUserType(
        IObjectWriterProvider objectWriterProvider
    )
    {
        return (UserType value, IParameterWriter parameterWriter) =>
            parameterWriter.Write(value.ToString());
    }
}

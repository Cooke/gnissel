using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal static partial class ObjectWriters
{
    public static Action<UserId, IParameterWriter> CreateWriteUserId(IObjectWriterProvider _) =>
        (value, parameterWriter) => parameterWriter.Write(value.Value);
}

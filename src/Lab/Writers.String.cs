using System.Collections.Immutable;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbWriters
    {
        public ObjectWriter<string?> StringWriter { get; init; }

        public ImmutableArray<WriteDescriptor> CreateWriteStringDescriptors() =>
            [new UnspecifiedColumnWriteDescriptor()];

        public void WriteString(string? value, IParameterWriter parameterWriter) =>
            parameterWriter.Write(value);
    }
}

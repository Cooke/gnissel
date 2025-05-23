using System.Collections.Immutable;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbWriters
    {
        public ObjectWriter<UserType> NonNullableUserTypeWriter { get; init; }

        public ObjectWriter<UserType?> UserTypeWriter { get; init; }

        private ImmutableArray<WriteDescriptor> CreateWriteUserTypeDescriptors() =>
            [new UnspecifiedColumnWriteDescriptor()];

        private void WriteUserType(UserType value, IParameterWriter parameterWriter) =>
            parameterWriter.Write(value.ToString());

        private void WriteUserType(UserType? value, IParameterWriter parameterWriter) =>
            parameterWriter.Write(value?.ToString());
    }
}

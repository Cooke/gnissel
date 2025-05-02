using System.Collections.Immutable;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbWriters
    {
        public ObjectWriter<Address?> AddressWriter { get; init; }

        private ImmutableArray<WriteDescriptor> CreateWriteAddressDescriptors() =>
            [.. StringWriter.WriteDescriptors.Select(x => x.WithParent(NameProvider, "Street"))];

        private void WriteAddress(Address? value, IParameterWriter parameterWriter)
        {
            StringWriter.Write(value?.Street, parameterWriter);
        }
    }
}

using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbReaders
    {
        private ImmutableArray<ReadDescriptor> CreateReadAddressDescriptors() =>
            [new NameReadDescriptor("city")];

        public ObjectReader<Address?> AddressReader { get; init; }

        private Address? ReadAddress(DbDataReader reader, OrdinalReader ordinalReader)
        {
            return null!;
        }
    }
}

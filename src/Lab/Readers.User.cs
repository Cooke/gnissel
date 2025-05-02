using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbReaders
    {
        public ObjectReader<User?> UserReader { get; init; }

        private ImmutableArray<ReadDescriptor> CreateReadUserDescriptors() =>
            [
                .. StringReader.ReadDescriptors.Select(d => d.WithParent(NameProvider, "Name")),
                .. AddressReader.ReadDescriptors.Select(d => d.WithParent(NameProvider, "Address")),
                .. UserTypeReader.ReadDescriptors.Select(d =>
                    d.WithParent(NameProvider, "UserType")
                ),
            ];

        private User? ReadUser(DbDataReader reader, OrdinalReader ordinalReader)
        {
            var name = reader.GetValueOrNull<string>(ordinalReader.Read());
            var address = AddressReader.Read(reader, ordinalReader);
            var userType = UserTypeReader.Read(reader, ordinalReader);

            if (name is null && address != null)
            {
                return null;
            }

            return new User(
                default!,
                default!,
                address!,
                userType ?? throw new InvalidOperationException("Expected non-null value")
            );
        }
    }
}

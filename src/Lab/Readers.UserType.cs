using System.Collections.Immutable;
using System.Data.Common;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbReaders
    {
        public ObjectReader<UserType?> UserTypeReader { get; init; }
        
        public ObjectReader<UserType> UserTypeNotNullReader { get; init; }

        private ImmutableArray<ReadDescriptor> CreateReadUserTypeDescriptors() =>
            [
                new NextOrdinalReadDescriptor()
            ];
        
        private UserType ReadNotNullUserType(DbDataReader reader, OrdinalReader ordinalReader)
        {
            return UserTypeReader.Read(reader, ordinalReader).Value;
        }

        private UserType? ReadUserType(DbDataReader reader, OrdinalReader ordinalReader)
        {
            var value = reader.GetValueOrNull<string>(ordinalReader.Read());
            return value is null ? null : Enum.TryParse<UserType>(value, out var result) ? result : null;
        }
    }
}

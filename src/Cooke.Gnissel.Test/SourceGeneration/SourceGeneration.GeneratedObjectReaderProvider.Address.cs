using System.Data.Common;
using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Address?> _addressReader;

        private static readonly MultiReaderMetadata ReadAddressMetadata =
            new(
                [
                    new NameReaderMetadata("street"),
                    new NameReaderMetadata("city"),
                    new NameReaderMetadata("state"),
                    new NameReaderMetadata("zip"),
                ]
            );

        private Address? ReadAddress(DbDataReader reader, OrdinalReader ordinalReader)
        {
            var street = reader.GetStringOrNull(ordinalReader.Read());
            var city = reader.GetStringOrNull(ordinalReader.Read());
            var state = reader.GetStringOrNull(ordinalReader.Read());
            var zip = reader.GetStringOrNull(ordinalReader.Read());

            if (street is null && city is null && state is null && zip is null)
            {
                return null;
            }

            return new(
                street ?? throw new InvalidOperationException(),
                city ?? throw new InvalidOperationException(),
                state ?? throw new InvalidOperationException(),
                zip ?? throw new InvalidOperationException()
            );
        }
    }
}

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
            if (ObjectReaderUtils.IsNull(reader, ordinalReader, _addressReader))
            {
                return null;
            }

            return new(
                reader.GetString(ordinalReader.Read()),
                reader.GetString(ordinalReader.Read()),
                reader.GetString(ordinalReader.Read()),
                reader.GetString(ordinalReader.Read())
            );
        }
    }
}

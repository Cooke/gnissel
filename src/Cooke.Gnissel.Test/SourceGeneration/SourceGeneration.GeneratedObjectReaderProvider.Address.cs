using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Address?> _addressReader = CreateObjectReader(
            adapter,
            ReadAddress,
            AddressPath
        );

        private static readonly MultiReaderDescriptor AddressPath =
            new(
                [
                    new NameReaderDescriptor("street"),
                    new NameReaderDescriptor("city"),
                    new NameReaderDescriptor("state"),
                    new NameReaderDescriptor("zip"),
                ]
            );

        private static Address? ReadAddress(DbDataReader reader, Ordinals ordinals)
        {
            if (reader.IsDBNull(ordinals[0]))
            {
                return null;
            }

            return new(
                reader.GetString(
                    ordinals[0] /* street */
                ),
                reader.GetString(
                    ordinals[1] /* city */
                ),
                reader.GetString(
                    ordinals[2] /* state */
                ),
                reader.GetString(
                    ordinals[3] /* zip */
                )
            );
        }
    }
}

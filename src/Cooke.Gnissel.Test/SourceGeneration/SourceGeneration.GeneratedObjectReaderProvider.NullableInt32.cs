using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Int32?> _nullableInt32Reader = CreateObjectReader(
            adapter,
            ReadNullableInt32,
            ReadNullableInt32Paths
        );

        private static readonly ReaderDescriptor ReadNullableInt32Paths =
            new PositionReaderDescriptor(0);

        private static Int32? ReadNullableInt32(DbDataReader reader, Ordinals ordinals)
        {
            if (IsAllDbNull(reader, ordinals))
            {
                return null;
            }

            return reader.GetInt32(ordinals[0]);
        }
    }
}

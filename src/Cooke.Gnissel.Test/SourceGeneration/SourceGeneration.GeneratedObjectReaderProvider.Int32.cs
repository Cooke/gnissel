using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Int32> _int32Reader = CreateObjectReader(
            adapter,
            ReadInt32,
            ReadInt32Paths
        );

        private static readonly ReaderDescriptor ReadInt32Paths = new PositionReaderDescriptor(0);

        private static Int32 ReadInt32(DbDataReader reader, Ordinals ordinals)
        {
            return reader.GetInt32(ordinals[0]);
        }
    }
}

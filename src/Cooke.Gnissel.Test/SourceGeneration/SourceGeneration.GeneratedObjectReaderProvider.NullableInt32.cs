using System.Data.Common;
using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Int32?> _nullableInt32Reader;

        private static readonly ReaderMetadata ReadNullableInt32Paths =
            new NextOrdinalReaderMetadata();

        private Int32? ReadNullableInt32(DbDataReader reader, OrdinalReader ordinalReader)
        {
            if (ObjectReaderUtils.IsNull(reader, ordinalReader, _nullableInt32Reader))
            {
                return null;
            }

            return reader.GetInt32(ordinalReader.Read());
        }
    }
}

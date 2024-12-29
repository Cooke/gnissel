using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Int32> _int32Reader;

        private static readonly ReaderMetadata ReadInt32Paths = new NextOrdinalReaderMetadata();

        private static Int32 ReadInt32(DbDataReader reader, OrdinalReader ordinalReader)
        {
            return reader.GetInt32(ordinalReader.Read());
        }
    }
}

using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<UserId?> _userIdReader;

        private static readonly ReaderMetadata ReadUserIdMetadata = new NextOrdinalReaderMetadata();

        private UserId? ReadUserId(DbDataReader reader, OrdinalReader ordinalReader)
        {
            if (IsNull(reader, ordinalReader, _userIdReader))
            {
                return null;
            }

            return new UserId(reader.GetInt32(ordinalReader.Read()));
        }
    }
}

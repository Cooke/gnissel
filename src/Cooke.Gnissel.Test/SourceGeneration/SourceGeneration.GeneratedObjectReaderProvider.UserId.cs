using System.Data.Common;
using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<UserId?> _userIdReader;

        private static readonly ReaderMetadata ReadUserIdMetadata = new NextOrdinalReaderMetadata();

        private UserId? ReadUserId(DbDataReader reader, OrdinalReader ordinalReader)
        {
            var value = reader.GetInt32OrNull(ordinalReader.Read());
            if (value is null)
            {
                return null;
            }

            return new(value.Value);
        }
    }
}

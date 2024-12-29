using System.Data.Common;
using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<User?> _userReader;

        private static ReaderMetadata ReadUserMetadata =>
            new MultiReaderMetadata(
                [
                    new NestedReaderMetadata("id", ReadUserIdMetadata),
                    new NameReaderMetadata("name"),
                    new NameReaderMetadata("age"),
                    new NestedReaderMetadata("address", ReadAddressMetadata),
                ]
            );

        private User? ReadUser(DbDataReader reader, OrdinalReader ordinalReader)
        {
            if (ObjectReaderUtils.IsNull(reader, ordinalReader, _userReader))
            {
                return null;
            }

            return new User(
                _userIdReader.Read(reader, ordinalReader) ?? throw new InvalidOperationException(),
                reader.GetString(ordinalReader.Read()),
                reader.GetInt32(ordinalReader.Read()),
                _addressReader.Read(reader, ordinalReader) ?? throw new InvalidOperationException()
            );
        }
    }
}

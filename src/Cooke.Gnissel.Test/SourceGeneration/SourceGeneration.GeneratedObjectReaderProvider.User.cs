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
            var userId = _userIdReader.Read(reader, ordinalReader);
            var name = reader.GetStringOrNull(ordinalReader.Read());
            var age = reader.GetInt32OrNull(ordinalReader.Read());
            var address = _addressReader.Read(reader, ordinalReader);

            if (userId is null && name is null && age is null && address is null)
            {
                return null;
            }

            return new User(
                userId ?? throw new InvalidOperationException(),
                name ?? throw new InvalidOperationException(),
                age ?? throw new InvalidOperationException(),
                address ?? throw new InvalidOperationException()
            );
        }
    }
}

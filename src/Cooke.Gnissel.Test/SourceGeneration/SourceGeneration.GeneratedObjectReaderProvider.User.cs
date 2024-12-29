using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<User?> _userReader = CreateObjectReader(
            adapter,
            ReadUser,
            ReadUserPaths
        );

        private static readonly ReaderDescriptor ReadUserPaths = new MultiReaderDescriptor(
            [
                new NestedReaderDescriptor("id", new ThunkReaderDescriptor(() => ReadUserIdPaths)),
                new NameReaderDescriptor("name"),
                new NameReaderDescriptor("age"),
                new NestedReaderDescriptor("address", new ThunkReaderDescriptor(() => AddressPath)),
            ]
        );

        private User? ReadUser(DbDataReader reader, Ordinals ordinals)
        {
            if (IsAllDbNull(reader, ordinals))
            {
                return null;
            }

            return new User(
                _userIdReader.Read(reader, ordinals[.._userIdReader.ReadDescriptors.Length])
                    ?? throw new InvalidOperationException(),
                reader.GetString(
                    ordinals[_userIdReader.ReadDescriptors.Length] /* name */
                ),
                reader.GetInt32(
                    ordinals[2] /* age */
                ),
                ReadAddress(
                    reader,
                    ordinals.Slice(3, 4) /* address */
                ) ?? throw new InvalidOperationException()
            );
        }
    }
}

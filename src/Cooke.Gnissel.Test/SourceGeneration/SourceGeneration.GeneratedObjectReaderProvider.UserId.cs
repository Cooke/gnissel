using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<UserId?> _userIdReader = CreateObjectReader(
            adapter,
            ReadUserId,
            ReadUserIdPaths
        );

        private static readonly ReaderDescriptor ReadUserIdPaths = new PositionReaderDescriptor(0);

        private static UserId? ReadUserId(DbDataReader reader, Ordinals ordinals)
        {
            if (IsAllDbNull(reader, ordinals))
            {
                return null;
            }

            return new UserId(reader.GetInt32(ordinals[0]));
        }
    }
}

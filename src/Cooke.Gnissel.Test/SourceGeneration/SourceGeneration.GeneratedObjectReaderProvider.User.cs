using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<User> _userReader = CreateObjectReader(
            adapter,
            ReadUser,
            ReadUserPaths
        );

        private static readonly ImmutableArray<PathSegment> ReadUserPaths =
        [
            new ParameterPathSegment("name"),
            new ParameterPathSegment("age"),
        ];

        private static User ReadUser(DbDataReader reader, IReadOnlyList<int> columnOrdinals)
        {
            return new User(
                reader.GetString(
                    columnOrdinals[0] /* name */
                ),
                reader.GetInt32(
                    columnOrdinals[0] /* age */
                )
            );
        }
    }
}

using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<User?> _nullableUserReader = CreateObjectReader(
            adapter,
            ReadNullableUser,
            ReadNullableUserPaths
        );

        private static readonly ImmutableArray<PathSegment> ReadNullableUserPaths =
        [
            new ParameterPathSegment("name"),
            new ParameterPathSegment("age"),
        ];

        private static User? ReadNullableUser(
            DbDataReader reader,
            IReadOnlyList<int> columnOrdinals
        )
        {
            if (reader.IsDBNull(0) && reader.IsDBNull(1))
            {
                return null;
            }

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

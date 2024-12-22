using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<(User, Device)> _tupleUserDeviceReader = CreateObjectReader(
            adapter,
            ReadTupleDeviceUser,
            ReadTupleDeviceUserPaths
        );

        private static readonly ImmutableArray<PathSegment> ReadTupleDeviceUserPaths =
        [
            new ParameterPathSegment("name"),
            new ParameterPathSegment("age"),
            new ParameterPathSegment("name"),
        ];

        private static (User, Device) ReadTupleDeviceUser(
            DbDataReader reader,
            IReadOnlyList<int> columnOrdinals
        )
        {
            return (
                new User(
                    reader.GetString(
                        columnOrdinals[0] /* name */
                    ),
                    reader.GetInt32(
                        columnOrdinals[1] /* age */
                    )
                ),
                new Device(reader.GetString(columnOrdinals[2]))
            );
        }
    }
}

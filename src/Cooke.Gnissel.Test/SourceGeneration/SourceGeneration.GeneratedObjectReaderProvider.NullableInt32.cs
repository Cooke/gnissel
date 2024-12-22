using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Int32?> _nullableInt32Reader = CreateObjectReader(
            adapter,
            ReadNullableInt32,
            ReadNullablleInt32Paths
        );

        private static readonly ImmutableArray<PathSegment> ReadNullablleInt32Paths = [];

        private static Int32? ReadNullableInt32(
            DbDataReader reader,
            IReadOnlyList<int> columnOrdinals
        ) => reader.IsDBNull(0) ? null : reader.GetInt32(0);
    }
}

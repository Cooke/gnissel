using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Int32> _int32Reader = CreateObjectReader(
            adapter,
            ReadInt32,
            ReadInt32Paths
        );

        private static readonly ImmutableArray<PathSegment> ReadInt32Paths = [];

        private static Int32 ReadInt32(DbDataReader reader, IReadOnlyList<int> columnOrdinals)
        {
            return reader.GetInt32(columnOrdinals[0]);
        }
    }
}

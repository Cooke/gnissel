using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Device> _deviceReader = CreateObjectReader<Device>(
            adapter,
            ReadDevice,
            [new ParameterPathSegment("name")]
        );

        private static Device ReadDevice(DbDataReader reader, IReadOnlyList<int> columnOrdinals)
        {
            return new(
                reader.GetString(
                    columnOrdinals[0] /* name */
                )
            );
        }
    }
}

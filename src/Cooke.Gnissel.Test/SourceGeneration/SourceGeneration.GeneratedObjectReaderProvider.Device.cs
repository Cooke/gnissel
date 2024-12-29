using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Device?> _deviceReader = CreateObjectReader(
            adapter,
            ReadDevice,
            DevicePath
        );

        private static readonly MultiReaderDescriptor DevicePath = new MultiReaderDescriptor(
            [new NameReaderDescriptor("name")]
        );

        private static Device? ReadDevice(DbDataReader reader, Ordinals ordinals)
        {
            if (IsAllDbNull(reader, ordinals))
            {
                return null;
            }

            return new(
                reader.GetString(
                    ordinals[0] /* name */
                )
            );
        }
    }
}

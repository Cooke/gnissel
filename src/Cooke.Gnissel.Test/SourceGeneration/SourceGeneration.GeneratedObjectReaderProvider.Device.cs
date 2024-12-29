using System.Data.Common;
using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Device?> _deviceReader;

        private static readonly MultiReaderMetadata ReadDeviceMetadata = new MultiReaderMetadata(
            [new NameReaderMetadata("name")]
        );

        private Device? ReadDevice(DbDataReader reader, OrdinalReader ordinalReader)
        {
            if (ObjectReaderUtils.IsNull(reader, ordinalReader, _deviceReader))
            {
                return null;
            }

            return new(reader.GetString(ordinalReader.Read()));
        }
    }
}

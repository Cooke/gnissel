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
            var name = reader.GetStringOrNull(ordinalReader.Read());
            if (name is null)
            {
                return null;
            }

            return new(name ?? throw new InvalidOperationException());
        }
    }
}

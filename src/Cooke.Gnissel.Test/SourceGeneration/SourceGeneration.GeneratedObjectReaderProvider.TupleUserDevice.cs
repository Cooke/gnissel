using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<(User?, Device?)> _tupleUserDeviceReader;

        private static ReaderMetadata ReadTupleDeviceUserMetadata =>
            new MultiReaderMetadata([ReadUserMetadata, ReadDeviceMetadata]);

        private (User?, Device?) ReadTupleDeviceUser(
            DbDataReader reader,
            OrdinalReader ordinalReader
        )
        {
            return (
                _userReader.Read(reader, ordinalReader),
                _deviceReader.Read(reader, ordinalReader)
            );
        }
    }
}

using System.Data.Common;

namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<(User?, Device?)> _tupleUserDeviceReader = CreateObjectReader(
            adapter,
            ReadTupleDeviceUser,
            ReadTupleDeviceUserPaths
        );

        private static readonly ReaderDescriptor ReadTupleDeviceUserPaths =
            new MultiReaderDescriptor(
                [
                    new ThunkReaderDescriptor(() => ReadUserPaths),
                    new ThunkReaderDescriptor(() => DevicePath),
                ]
            );

        private static (User?, Device?) ReadTupleDeviceUser(DbDataReader reader, Ordinals ordinals)
        {
            return (ReadUser(reader, ordinals[..7]), ReadDevice(reader, ordinals[7..]));
        }
    }
}

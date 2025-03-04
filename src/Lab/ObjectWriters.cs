using System.Collections.Immutable;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal static partial class ObjectWriters
{
    static ObjectWriters()
    {
        Descriptors = [GeoLocationWriterDescriptor];
    }

    public static readonly ImmutableArray<IObjectReaderDescriptor> Descriptors;
}

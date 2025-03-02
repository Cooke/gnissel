using System.Collections.Immutable;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal static partial class ObjectReaders
{
    static ObjectReaders()
    {
        Descriptors = [UserReaderDescriptor, .. CreateAnons()];
    }

    public static readonly ImmutableArray<IObjectReaderDescriptor> Descriptors;
}

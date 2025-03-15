using System.Collections.Immutable;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal static partial class ObjectReaders
{
    static ObjectReaders()
    {
        Descriptors = CreateReaderMappers();
    }

    private static ImmutableArray<IObjectReaderDescriptor> CreateReaderMappers()
    {
        return [UserReaderDescriptor, .. CreateAnons()];
    }

    public static readonly ImmutableArray<IObjectReaderDescriptor> Descriptors;
}

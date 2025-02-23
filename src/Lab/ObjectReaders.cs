using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.SourceGeneration;

namespace Gnissel.SourceGeneration;

internal static partial class ObjectReaders
{
    public static IObjectReaderProvider CreateProvider(IDbAdapter adapter) =>
        new ObjectReaderProviderBuilder(Descriptors).Build(adapter);

    static ObjectReaders()
    {
        Descriptors = [UserReaderDescriptor, .. CreateAnons()];
    }

    public static readonly ImmutableArray<IObjectReaderDescriptor> Descriptors;

    [ModuleInitializer]
    internal static void Initialize()
    {
        GlobalObjectReaders.Add(Descriptors);
    }
}

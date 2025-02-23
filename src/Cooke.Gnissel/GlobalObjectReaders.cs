using System.Collections.Concurrent;

namespace Cooke.Gnissel;

public static class GlobalObjectReaders
{
    public static readonly ConcurrentBag<IObjectReaderDescriptor> Descriptors = new();

    public static void Add(IEnumerable<IObjectReaderDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            Descriptors.Add(descriptor);
        }
    }
}

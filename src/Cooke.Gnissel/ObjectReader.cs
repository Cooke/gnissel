using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel;

public interface IObjectReader
{
    int ReadDescriptorsCount { get; }
}

public class ObjectReader<TOut>(
    ObjectReaderFunc<TOut> read,
    ImmutableArray<ReadDescriptor> readDescriptors
) : IObjectReader
{
    public ObjectReaderFunc<TOut> Read { get; } = read;

    public ImmutableArray<ReadDescriptor> ReadDescriptors { get; } = readDescriptors;

    int IObjectReader.ReadDescriptorsCount => ReadDescriptors.Length;
}

public abstract record ReadDescriptor;

public record NextOrdinalReadDescriptor : ReadDescriptor;

public record NameReadDescriptor(string Name) : ReadDescriptor;

public static class ObjectReaderUtils
{
    public static bool IsNull(
        DbDataReader reader,
        OrdinalReader ordinalReader,
        IObjectReader objectReader
    )
    {
        var snapshot = ordinalReader.CreateSnapshot();
        for (var i = 0; i < objectReader.ReadDescriptorsCount; i++)
        {
            if (!reader.IsDBNull(snapshot.Read()))
            {
                return false;
            }
        }

        return true;
    }
}

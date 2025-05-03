using System.Collections.Immutable;
using System.Data.Common;

namespace Cooke.Gnissel;

public interface IObjectReader
{
    public static IObjectReader Create<T>(
        ObjectReaderFunc<T> read,
        Func<ImmutableArray<ReadDescriptor>> readDescriptorsFunc
    ) => new ObjectReader<T>(read, readDescriptorsFunc);

    Type ObjectType { get; }

    ImmutableArray<ReadDescriptor> ReadDescriptors { get; }
}

public delegate TOut ObjectReaderFunc<out TOut>(
    DbDataReader dataReader,
    OrdinalReader ordinalReader
);

public class OrdinalReader(int[] ordinals)
{
    private int _index = 0;

    public int Read() => ordinals[_index++];

    public void Reset() => _index = 0;
}

public class ObjectReader<TOut>(
    ObjectReaderFunc<TOut> read,
    Func<ImmutableArray<ReadDescriptor>> readDescriptorsFunc
) : IObjectReader
{
    private ImmutableArray<ReadDescriptor>? _readDescriptors;

    public ObjectReaderFunc<TOut> Read { get; } = read;

    public ImmutableArray<ReadDescriptor> ReadDescriptors
    {
        get
        {
            _readDescriptors ??= readDescriptorsFunc();
            return _readDescriptors.Value;
        }
    }

    public Type ObjectType => typeof(TOut);
}

public abstract record ReadDescriptor
{
    public abstract ReadDescriptor WithParent(IDbNameProvider nameProvider, string parentMember);

    public abstract ReadDescriptor WithParent(
        IDbNameProvider nameProvider,
        string parentName,
        string parentMember
    );
}

public record NextOrdinalReadDescriptor : ReadDescriptor
{
    public override ReadDescriptor WithParent(IDbNameProvider nameProvider, string parentMember) =>
        new NameReadDescriptor(nameProvider.ToColumnName(parentMember), [parentMember]);

    public override ReadDescriptor WithParent(
        IDbNameProvider nameProvider,
        string parentName,
        string parentMember
    ) => new NameReadDescriptor(parentName, [parentMember]);
}

public record NameReadDescriptor(string Name, ImmutableArray<string> MemberChain) : ReadDescriptor
{
    public override ReadDescriptor WithParent(IDbNameProvider nameProvider, string parentMember) =>
        new NameReadDescriptor(
            nameProvider.CombineColumnNames(nameProvider.ToColumnName(parentMember), Name),
            [parentMember, .. MemberChain]
        );

    public override ReadDescriptor WithParent(
        IDbNameProvider nameProvider,
        string parentName,
        string parentMember
    ) =>
        new NameReadDescriptor(
            nameProvider.CombineColumnNames(parentName, Name),
            [parentMember, .. MemberChain]
        );
}

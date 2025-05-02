using System.Collections.Immutable;

namespace Cooke.Gnissel;

public delegate void ObjectWriterFunc<in T>(T value, IParameterWriter parameterWriter);

public interface IObjectWriter
{
    Type ObjectType { get; }

    void Write(object? value, IParameterWriter parameterWriter);

    ImmutableArray<WriteDescriptor> WriteDescriptors { get; }
}

public class ObjectWriter<T>(
    ObjectWriterFunc<T> write,
    Func<ImmutableArray<WriteDescriptor>> getWriteDescriptors
) : IObjectWriter
{
    private ImmutableArray<WriteDescriptor>? _writeDescriptors;
    public ObjectWriterFunc<T> Write { get; } = write;

    public Type ObjectType { get; } = typeof(T);

    public ImmutableArray<WriteDescriptor> WriteDescriptors
    {
        get
        {
            _writeDescriptors ??= getWriteDescriptors();
            return _writeDescriptors.Value;
        }
    }

    void IObjectWriter.Write(object? value, IParameterWriter parameterWriter)
    {
        if (value is not T valueAsT)
        {
            throw new InvalidOperationException(
                $"Cannot write value of type {value?.GetType()} to {typeof(T)}"
            );
        }

        Write(valueAsT, parameterWriter);
    }
}

public abstract record WriteDescriptor
{
    public abstract WriteDescriptor WithParent(IDbNameProvider nameProvider, string parentMember);
}

public record UnspecifiedColumnWriteDescriptor : WriteDescriptor
{
    public override WriteDescriptor WithParent(IDbNameProvider nameProvider, string parentMember) =>
        new ColumnWriteDescriptor(nameProvider.ToColumnName([parentMember]), [parentMember]);
}

public record ColumnWriteDescriptor(string Name, ImmutableArray<string> MemberChain)
    : WriteDescriptor
{
    public override WriteDescriptor WithParent(IDbNameProvider nameProvider, string parentMember)
    {
        ImmutableArray<string> newPropertyChain = [parentMember, .. MemberChain];
        return new ColumnWriteDescriptor(
            nameProvider.ToColumnName(newPropertyChain),
            newPropertyChain
        );
    }
}

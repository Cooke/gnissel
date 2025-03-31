namespace Cooke.Gnissel;

public delegate void ObjectWriterFunc<in T>(T value, IParameterWriter parameterWriter);

public interface IObjectWriter
{
    Type ObjectType { get; }

    // ImmutableArray<WriteDescriptor> WriteDescriptors { get; }
}

public class ObjectWriter<T>(ObjectWriterFunc<T> write) : IObjectWriter
{
    public ObjectWriterFunc<T> Write { get; } = write;

    public Type ObjectType { get; } = typeof(T);
}

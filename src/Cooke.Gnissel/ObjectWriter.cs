namespace Cooke.Gnissel;

public delegate void ObjectWriterFunc<in T>(T value, IParameterWriter parameterWriter);

public interface IObjectWriter
{
    Type ObjectType { get; }

    void Write(object? value, IParameterWriter parameterWriter);

    // ImmutableArray<WriteDescriptor> WriteDescriptors { get; }
}

public class ObjectWriter<T>(ObjectWriterFunc<T> write) : IObjectWriter
{
    public ObjectWriterFunc<T> Write { get; } = write;

    public Type ObjectType { get; } = typeof(T);

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

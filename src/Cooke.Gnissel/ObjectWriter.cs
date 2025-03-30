namespace Cooke.Gnissel;

public static class ObjectWriterUtils
{
    public static ObjectWriter<T?> CreateDefault<T>() =>
        new((value, parameterWriter) => parameterWriter.Write(value));

    public static ObjectWriter<T> CreateNonNullableVariant<T>(
        Func<ObjectWriter<T?>> nullableWriterGetter
    )
        where T : struct =>
        new(
            (value, parameterWriter) =>
            {
                nullableWriterGetter().Write(value, parameterWriter);
            }
        );
}

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

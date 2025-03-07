namespace Cooke.Gnissel;

public class ObjectWriter<T>(ObjectWriterFunc<T> write) : IObjectWriter
{
    public ObjectWriterFunc<T> Write { get; } = write;

    public Type ObjectType { get; } = typeof(T);
}

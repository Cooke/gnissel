using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel;

public interface IObjectWriterDescriptor : IObjectMapperDescriptor
{
    public IObjectWriter Create(ObjectWriterCreateContext context);

    Type ObjectType { get; }
}

public class WriteMapperDescriptor<T>(Func<ObjectWriterCreateContext, ObjectWriterFunc<T>> factory)
    : IObjectWriterDescriptor
{
    public IObjectWriter Create(ObjectWriterCreateContext context) =>
        new ObjectWriter<T>(factory(context));

    public Type ObjectType { get; } = typeof(T);
}

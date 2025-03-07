using Cooke.Gnissel.SourceGeneration;

namespace Cooke.Gnissel;

public interface IObjectWriterDescriptor : IObjectMapperDescriptor
{
    public IObjectWriter Create(ObjectWriterCreateContext context);

    Type ObjectType { get; }

    ObjectReaderMetadata Metadata { get; }
}

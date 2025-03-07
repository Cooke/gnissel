namespace Cooke.Gnissel;

public interface IObjectWriter
{
    Type ObjectType { get; }

    // ImmutableArray<WriteDescriptor> WriteDescriptors { get; }
}

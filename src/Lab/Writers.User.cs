using System.Collections.Immutable;
using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbWriters
    {
        public ObjectWriter<User?> UserWriter { get; init; }

        private ImmutableArray<WriteDescriptor> CreateWriteUserDescriptors() =>
            [
                .. UserIdWriter.WriteDescriptors.Select(x => x.WithParent(NameProvider, "Id")),
                .. StringWriter.WriteDescriptors.Select(x => x.WithParent(NameProvider, "Name")),
                .. AddressWriter.WriteDescriptors.Select(x =>
                    x.WithParent(NameProvider, "Address")
                ),
                .. UserTypeWriter.WriteDescriptors.Select(x => x.WithParent(NameProvider, "Type")),
            ];

        public void WriteUser(User? user, IParameterWriter parameterWriter)
        {
            UserIdWriter.Write(user?.Id, parameterWriter);
            StringWriter.Write(user?.Name, parameterWriter);
            AddressWriter.Write(user?.Address, parameterWriter);
            UserTypeWriter.Write(user?.Type, parameterWriter);
        }
    }
}

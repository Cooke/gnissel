using System.Collections.Immutable;
using Cooke.Gnissel;
using Cooke.Gnissel.Services;

namespace Gnissel.SourceGeneration;

public interface IReaders
{
    ImmutableArray<IObjectReader> All { get; }
}

public interface IMappers
{
    IReaders Readers { get; }
}

internal partial class DbMappers : IMappers
{
    public DbReaders Readers { get; init; } = new DbReaders();

    IReaders IMappers.Readers => Readers;

    internal partial class DbReaders : IReaders
    {
        public DbReaders()
        {
            UserReader = new ObjectReader<User?>(ReadUser, CreateReadUserDescriptors);
            AddressReader = new ObjectReader<Address?>(ReadAddress, CreateReadAddressDescriptors);
        }

        public ImmutableArray<IObjectReader> All => [UserReader, AddressReader];
    }
}

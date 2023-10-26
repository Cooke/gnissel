using System.Data.Common;

namespace Cooke.Gnissel;

internal class ManagedConnectionCommandFactory : ICommandFactory
{
    private readonly IDbAdapter _dbAdapter;

    public ManagedConnectionCommandFactory(IDbAdapter dbAdapter)
    {
        _dbAdapter = dbAdapter;
    }

    public DbCommand CreateCommand() => _dbAdapter.CreateManagedConnectionCommand();
}

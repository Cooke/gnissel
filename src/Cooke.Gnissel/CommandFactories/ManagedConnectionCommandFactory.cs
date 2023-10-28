using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.CommandFactories;

internal class ManagedConnectionCommandFactory : ICommandFactory
{
    private readonly IDbAdapter _dbAdapter;

    public ManagedConnectionCommandFactory(IDbAdapter dbAdapter)
    {
        _dbAdapter = dbAdapter;
    }

    public DbCommand CreateCommand() => _dbAdapter.CreateManagedConnectionCommand();
}

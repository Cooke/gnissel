using System.Data.Common;

namespace Cooke.Gnissel;

internal class ReadyCommandProvider : ICommandProvider
{
    private readonly IDbAdapter _dbAdapter;

    public ReadyCommandProvider(IDbAdapter dbAdapter)
    {
        _dbAdapter = dbAdapter;
    }

    public DbCommand CreateCommand() => _dbAdapter.CreateReadyCommand();
}

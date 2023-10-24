using System.Data.Common;

namespace Cooke.Gnissel;

internal interface ICommandProvider
{
    DbCommand CreateCommand();
}

internal class ReadyCommandProvider : ICommandProvider
{
    private readonly DbAdapter _dbAdapter;

    public ReadyCommandProvider(DbAdapter dbAdapter)
    {
        _dbAdapter = dbAdapter;
    }

    public DbCommand CreateCommand() => _dbAdapter.CreateReadyCommand();
}

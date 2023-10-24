using System.Data.Common;

namespace Cooke.Gnissel;

public interface ICommandProvider
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

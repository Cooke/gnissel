using System.Data.Common;

namespace Cooke.Gnissel;

public interface ICommandProvider
{
    DbCommand CreateCommand();
}

internal class ReadyCommandProvider : ICommandProvider
{
    private readonly IDbAdapter _dbAdapter;

    public ReadyCommandProvider(IDbAdapter dbAdapter)
    {
        _dbAdapter = dbAdapter;
    }

    public DbCommand CreateCommand() => _dbAdapter.CreateReadyCommand();
}

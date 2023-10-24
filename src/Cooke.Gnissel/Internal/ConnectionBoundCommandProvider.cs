using System.Data.Common;

namespace Cooke.Gnissel;

public sealed class ConnectionBoundCommandProvider : ICommandProvider
{
    private readonly DbConnection _connection;
    private readonly IDbAdapter _adapter;

    public ConnectionBoundCommandProvider(DbConnection connection, IDbAdapter adapter)
    {
        _connection = connection;
        _adapter = adapter;
    }

    public DbCommand CreateCommand()
    {
        var cmd = _adapter.CreateEmptyCommand();
        cmd.Connection = _connection;
        return cmd;
    }
}

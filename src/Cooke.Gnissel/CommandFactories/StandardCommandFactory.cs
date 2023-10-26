using System.Data.Common;

namespace Cooke.Gnissel;

public sealed class StandardCommandFactory : ICommandFactory
{
    private readonly DbConnection _connection;
    private readonly IDbAdapter _adapter;

    public StandardCommandFactory(DbConnection connection, IDbAdapter adapter)
    {
        _connection = connection;
        _adapter = adapter;
    }

    public DbCommand CreateCommand()
    {
        var cmd = _adapter.CreateCommand();
        cmd.Connection = _connection;
        return cmd;
    }
}

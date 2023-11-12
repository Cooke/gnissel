using System.Data.Common;

namespace Cooke.Gnissel.Services.Implementations;

public sealed class FixedConnectionDbConnector : IDbConnector
{
    private readonly DbConnection _connection;
    private readonly IDbAdapter _adapter;

    public FixedConnectionDbConnector(DbConnection connection, IDbAdapter adapter)
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

    public DbBatch CreateBatch() => _connection.CreateBatch();

    public DbConnection CreateConnection() => _connection;
}

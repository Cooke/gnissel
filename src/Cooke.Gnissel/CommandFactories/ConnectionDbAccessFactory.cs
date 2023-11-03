using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.CommandFactories;

public sealed class ConnectionDbAccessFactory : IDbAccessFactory
{
    private readonly DbConnection _connection;
    private readonly IDbAdapter _adapter;

    public ConnectionDbAccessFactory(DbConnection connection, IDbAdapter adapter)
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

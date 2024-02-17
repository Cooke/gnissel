using System.Data.Common;

namespace Cooke.Gnissel.Services.Implementations;

public sealed class FixedConnectionDbConnector(DbConnection connection, IDbAdapter adapter) : IDbConnector
{
    public DbCommand CreateCommand()
    {
        var cmd = adapter.CreateCommand();
        cmd.Connection = connection;
        return cmd;
    }

    public DbBatch CreateBatch() => connection.CreateBatch();

    public DbConnection CreateConnection() => connection;
}

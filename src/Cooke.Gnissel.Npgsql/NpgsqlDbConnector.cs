using System.Data.Common;
using Cooke.Gnissel.Services;
using Npgsql;

namespace Cooke.Gnissel.Npgsql;

public class NpgsqlDbConnector : IDbConnector
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlDbConnector(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public DbCommand CreateCommand() => _dataSource.CreateCommand();

    public DbBatch CreateBatch() => _dataSource.CreateBatch();

    public DbConnection CreateConnection() => _dataSource.CreateConnection();
}

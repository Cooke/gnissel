using System.Data.Common;
using Cooke.Gnissel.CommandFactories;
using Npgsql;

namespace Cooke.Gnissel.Npgsql;

public class NpgsqlDbAccessFactory : IDbAccessFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlDbAccessFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public DbCommand CreateCommand() => _dataSource.CreateCommand();

    public DbBatch CreateBatch() => _dataSource.CreateBatch();

    public DbConnection CreateConnection() => _dataSource.CreateConnection();
}

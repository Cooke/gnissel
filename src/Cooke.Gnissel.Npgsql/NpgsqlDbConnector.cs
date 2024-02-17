using System.Data.Common;
using Cooke.Gnissel.Services;
using Npgsql;

namespace Cooke.Gnissel.Npgsql;

public class NpgsqlDbConnector(NpgsqlDataSource dataSource) : IDbConnector
{
    public DbCommand CreateCommand() => dataSource.CreateCommand();

    public DbBatch CreateBatch() => dataSource.CreateBatch();

    public DbConnection CreateConnection() => dataSource.CreateConnection();
}

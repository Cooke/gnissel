using System.Data.Common;
using System.Reflection;
using Npgsql;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public sealed class NpgsqlDbConnectionProvider : DbConnectionProvider
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlDbConnectionProvider(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public DbCommand GetCommand() => _dataSource.CreateCommand();
}

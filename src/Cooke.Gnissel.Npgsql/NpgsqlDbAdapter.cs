using System.Data.Common;
using Npgsql;

namespace Cooke.Gnissel.Npgsql;

public sealed class NpgsqlDbAdapter : DbAdapter
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlDbAdapter(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public string EscapeIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    public DbParameter CreateParameter<TValue>(TValue value) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value }
            : new NpgsqlParameter<TValue> { TypedValue = value };

    public DbCommand CreateCommand() => _dataSource.CreateCommand();

    public DbCommand CreateCommand(DbConnection connection)
    {
        var command = new NpgsqlCommand();
        command.Connection = (NpgsqlConnection)connection;
        return command;
    }

    public async Task<DbConnection> OpenConnection() => await _dataSource.OpenConnectionAsync();
}

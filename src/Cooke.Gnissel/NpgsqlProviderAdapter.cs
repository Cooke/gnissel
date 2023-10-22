using System.Data.Common;
using Npgsql;

namespace Cooke.Gnissel;

public sealed class NpgsqlProviderAdapter : ProviderAdapter
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlProviderAdapter(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public string EscapeIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    public DbParameter CreateParameter<TValue>(TValue value) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value }
            : new NpgsqlParameter<TValue> { TypedValue = value };

    public DbCommand CreateCommand() => _dataSource.CreateCommand();
}

using System.Data.Common;
using System.Reflection;
using Npgsql;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public sealed class NpgsqlDbAdapter : IDbAdapter
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
    
    public DbConnection CreateConnection() => _dataSource.CreateConnection();

    public DbCommand CreateManagedConnectionCommand() => _dataSource.CreateCommand();

    public DbCommand CreateCommand() => new NpgsqlCommand();

    public string GetColumnName(PropertyInfo propertyInfo) =>
        NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(propertyInfo.Name);
}

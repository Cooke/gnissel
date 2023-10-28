#region

using System.Data.Common;
using System.Globalization;
using System.Reflection;
using Cooke.Gnissel.Services;
using Npgsql;
using Npgsql.NameTranslation;

#endregion

namespace Cooke.Gnissel.Npgsql;

public sealed class NpgsqlDbAdapter : IDbAdapter
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlDbAdapter(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public string EscapeIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    public DbParameter CreateParameter<TValue>(TValue value, string? dbType) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value, DataTypeName = dbType }
            : new NpgsqlParameter<TValue> { TypedValue = value, DataTypeName = dbType };

    public DbConnection CreateConnection() => _dataSource.CreateConnection();

    public DbCommand CreateManagedConnectionCommand() => _dataSource.CreateCommand();

    public DbCommand CreateCommand() => new NpgsqlCommand();

    public string GetColumnName(PropertyInfo propertyInfo) =>
        NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(propertyInfo.Name, CultureInfo.CurrentCulture);
}

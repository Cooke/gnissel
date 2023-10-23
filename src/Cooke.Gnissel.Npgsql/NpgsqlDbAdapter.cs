using System.Data.Common;
using System.Reflection;
using Npgsql;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public sealed class NpgsqlDbAdapter : DbAdapter
{
    public string EscapeIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    public DbParameter CreateParameter<TValue>(TValue value) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value }
            : new NpgsqlParameter<TValue> { TypedValue = value };

    public string GetColumnName(PropertyInfo propertyInfo) =>
        NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(propertyInfo.Name);
}

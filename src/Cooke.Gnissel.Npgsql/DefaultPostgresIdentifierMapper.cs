using System.Globalization;
using System.Reflection;
using Cooke.Gnissel.Services;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public class DefaultPostgresIdentifierMapper : IIdentifierMapper
{
    public string ToColumnName(ParameterInfo parameterInfo) =>
        ConvertToSnakeCase(parameterInfo.Name);

    public string ToColumnName(PropertyInfo propertyInfo) => ConvertToSnakeCase(propertyInfo.Name);

    public string ToTableName(Type type) => ConvertToSnakeCase(type.Name) + "s";

    private static string ConvertToSnakeCase(string? name)
    {
        return NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(
            name ?? throw new InvalidOperationException(),
            CultureInfo.CurrentCulture
        );
    }
}

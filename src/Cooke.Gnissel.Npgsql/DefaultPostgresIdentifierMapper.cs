using System.Globalization;
using System.Reflection;
using Cooke.Gnissel.Services;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public class DefaultPostgresIdentifierMapper : IIdentifierMapper
{
    public string ToColumnName(ParameterInfo parameterInfo) =>
        NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(
            parameterInfo.Name ?? throw new InvalidOperationException(),
            CultureInfo.CurrentCulture
        );

    public string ToColumnName(PropertyInfo propertyInfo) =>
        NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(
            propertyInfo.Name ?? throw new InvalidOperationException(),
            CultureInfo.CurrentCulture
        );
}

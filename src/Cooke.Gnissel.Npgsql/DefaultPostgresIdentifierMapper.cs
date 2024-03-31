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

    public string ToColumnName(IEnumerable<IIdentifierMapper.IdentifierPart> path) =>
        string.Join(
            ".",
            path.Select(
                part =>
                    part switch
                    {
                        IIdentifierMapper.ParameterPart parameterPart
                            => ToColumnName(parameterPart.ParameterInfo),
                        IIdentifierMapper.PropertyPart propertyPart
                            => ToColumnName(propertyPart.PropertyInfo),
                        _ => throw new InvalidOperationException()
                    }
            )
        );

    public string ToTableName(Type type) => ConvertToSnakeCase(type.Name) + "s";

    private static string ConvertToSnakeCase(string? name)
    {
        return NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(
            name ?? throw new InvalidOperationException(),
            CultureInfo.CurrentCulture
        );
    }
}

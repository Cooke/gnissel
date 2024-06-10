using System.Globalization;
using System.Reflection;
using Cooke.Gnissel.Services;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public class GnisselNpgsqlSnakeCaseNameProvider : IGnisselNpgsqlNameProvider
{
    private string ToColumnName(ParameterInfo parameterInfo) =>
        ConvertToSnakeCase(parameterInfo.Name);

    private string ToColumnName(PropertyInfo propertyInfo) => ConvertToSnakeCase(propertyInfo.Name);

    public string ToColumnName(IEnumerable<ObjectPathPart> path) =>
        string.Join(
            "_",
            path.Select(part =>
                part switch
                {
                    ParameterPathPart parameterPart => ToColumnName(parameterPart.ParameterInfo),
                    PropertyPathPart propertyPart => ToColumnName(propertyPart.PropertyInfo),
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
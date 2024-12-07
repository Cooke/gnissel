using System.Globalization;
using System.Reflection;
using Cooke.Gnissel.Services;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public class GnisselNpgsqlSnakeCaseNameProvider : IGnisselNpgsqlNameProvider
{
    public string ToColumnName(PathSegment part)
    {
        return part switch
        {
            ParameterPathSegment parameterPart => ConvertToSnakeCase(parameterPart.Name),
            PropertyPathSegment propertyPart => ConvertToSnakeCase(propertyPart.Name),
            NestedPathSegment nestedPart => ToColumnName(nestedPart.Parent)
                + "_"
                + ToColumnName(nestedPart.Child),
            _ => throw new InvalidOperationException(),
        };
    }

    public string ToTableName(Type type) => ConvertToSnakeCase(type.Name) + "s";

    private static string ConvertToSnakeCase(string? name)
    {
        return NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(
            name ?? throw new InvalidOperationException(),
            CultureInfo.CurrentCulture
        );
    }
}

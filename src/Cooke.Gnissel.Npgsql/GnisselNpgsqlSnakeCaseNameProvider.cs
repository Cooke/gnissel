using System.Globalization;
using Npgsql.NameTranslation;

namespace Cooke.Gnissel.Npgsql;

public class GnisselNpgsqlSnakeCaseNameProvider : IGnisselNpgsqlNameProvider
{
    public string ToColumnName(IEnumerable<string> part) =>
        string.Join("_", part.Select(ConvertToSnakeCase));

    public string ToTableName(Type type) => ConvertToSnakeCase(type.Name) + "s";

    private static string ConvertToSnakeCase(string? name) =>
        NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(
            name ?? throw new InvalidOperationException(),
            CultureInfo.CurrentCulture
        );
}

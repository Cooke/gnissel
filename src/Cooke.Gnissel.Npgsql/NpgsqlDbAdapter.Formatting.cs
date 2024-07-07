using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace Cooke.Gnissel.Npgsql;

public partial class NpgsqlDbAdapter
{
    public RenderedSql RenderSql(Sql sql, DbOptions options)
    {
        var sb = new StringBuilder(
            sql.Fragments.Sum(x =>
                x switch
                {
                    Sql.Literal { Value: var s } => s.Length,
                    Sql.Parameter => 3,
                    Sql.Identifier { Value: var s } => s.Length + 2,
                    _ => 0
                }
            )
        );

        var parameters = new List<DbParameter>();
        foreach (var fragment in sql.Fragments)
        {
            switch (fragment)
            {
                case Sql.Literal { Value: var s }:
                    sb.Append(s);
                    break;

                case Sql.Parameter p:
                    sb.Append('$');
                    sb.Append(parameters.Count + 1);
                    parameters.Add(p.CreateParameter(options));
                    break;

                case Sql.Identifier { Value: var identifier }:
                    sb.Append(EscapeIdentifierIfNeeded(identifier));
                    break;

                case Sql.LiteralValue { Value: var value }:
                    sb.Append(FormatLiteralValue(value, options));
                    break;
            }
        }

        return new RenderedSql(sb.ToString(), parameters.ToArray());
    }

    private static string FormatLiteralValue(object? value, DbOptions dbOptions)
    {
        if (value is null)
        {
            return "NULL";
        }

        var converter = dbOptions.GetConverter(value.GetType());
        if (converter != null)
        {
            // IMPROVEMENT: remove boxing of Value
            return FormatLiteralValue(converter.ToValue(value).Value);
        }

        return FormatLiteralValue(value);
    }

    private static string FormatLiteralValue(object? value) =>
        value switch
        {
            null => "NULL",
            string str => FormatString(str),
            _ => value.ToString()
        } ?? throw new InvalidOperationException();

    private static string FormatString(string strValue) => $"'{strValue}'";

    private static string EscapeIdentifierIfNeeded(string identifier) =>
        Regex.IsMatch(identifier, @"[^a-z0-9_]")
            ? $"\"{identifier.Replace("\"", "\"\"")}\""
            : identifier;
}

using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace Cooke.Gnissel.Npgsql;

public partial class NpgsqlDbAdapter
{
    public RenderedSql RenderSql(Sql sql, DbOptions options)
    {
        var estimatedQueryLength = 0;
        var numParameters = 0;
        foreach (var x in sql.Fragments)
            switch (x)
            {
                case Sql.Literal { Value: var s }:
                    estimatedQueryLength += s.Length;
                    break;
                case Sql.Parameter:
                    estimatedQueryLength += 4;
                    numParameters++;
                    break;
                case Sql.Identifier { Value: var s }:
                    estimatedQueryLength += s.Length + 2;
                    break;
                case Sql.LiteralValue:
                    estimatedQueryLength += 20;
                    break;
                default:
                    estimatedQueryLength += 0;
                    break;
            }

        var sb = new StringBuilder(estimatedQueryLength);

        var parameterWriter = new ListParameterWriter(options, numParameters);
        foreach (var fragment in sql.Fragments)
        {
            switch (fragment)
            {
                case Sql.Literal { Value: var s }:
                    sb.Append(s);
                    break;

                case Sql.Parameter p:
                    sb.Append('$');
                    sb.Append(parameterWriter.Parameters.Count + 1);
                    p.WriteParameter(parameterWriter);
                    break;

                case Sql.Identifier { Value: var identifier }:
                    sb.Append(EscapeIdentifierIfNeeded(identifier));
                    break;

                case Sql.LiteralValue { Value: var value }:
                    sb.Append(FormatLiteralValue(value, options));
                    break;
            }
        }

        return new RenderedSql(sb.ToString(), parameterWriter.Parameters.ToArray());
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
            _ => value.ToString(),
        } ?? throw new InvalidOperationException();

    private static string FormatString(string strValue) => $"'{strValue}'";

    private static string EscapeIdentifierIfNeeded(string identifier) =>
        Regex.IsMatch(identifier, @"[^a-z0-9_]")
            ? $"\"{identifier.Replace("\"", "\"\"")}\""
            : identifier;

    private class ListParameterWriter(DbOptions options, int numParameters) : IParameterWriter
    {
        public List<DbParameter> Parameters { get; } = new(numParameters);

        public void Write<T>(T value, string? dbType = null) =>
            Parameters.Add(options.CreateParameter(value, dbType));
    }
}

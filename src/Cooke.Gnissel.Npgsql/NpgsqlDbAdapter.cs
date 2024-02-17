#region

using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Cooke.Gnissel.Services;
using Npgsql;
using Npgsql.NameTranslation;

#endregion

namespace Cooke.Gnissel.Npgsql;

public sealed class NpgsqlDbAdapter(NpgsqlDataSource dataSource) : IDbAdapter
{
    public string EscapeIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    public string EscapeIdentifierIfNeeded(string identifier) =>
        Regex.IsMatch(identifier, @"[^a-zA-Z0-9_]")
            ? $"\"{identifier.Replace("\"", "\"\"")}\""
            : identifier;

    public DbParameter CreateParameter<TValue>(TValue value, string? dbType) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value, DataTypeName = dbType }
            : new NpgsqlParameter<TValue> { TypedValue = value, DataTypeName = dbType };

    public RenderedSql RenderSql(Sql sql)
    {
        var sb = new StringBuilder(
            sql.Fragments.Sum(
                x =>
                    x switch
                    {
                        Sql.Literal { Value: var s } => s.Length,
                        Sql.IParameter => 3,
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

                case Sql.IParameter p:
                    sb.Append('$');
                    sb.Append(parameters.Count + 1);
                    parameters.Add(p.ToParameter(this));
                    break;

                case Sql.Identifier { Value: var identifier }:
                    sb.Append(EscapeIdentifierIfNeeded(identifier));
                    break;
            }
        }

        return new RenderedSql(sb.ToString(), parameters.ToArray());
    }

    public DbBatchCommand CreateBatchCommand() => new NpgsqlBatchCommand();

    public DbCommand CreateCommand() => new NpgsqlCommand();

    public IDbConnector CreateConnector() => new NpgsqlDbConnector(dataSource);

    public IIdentifierMapper DefaultIdentifierMapper { get; } =
        new DefaultPostgresIdentifierMapper();
}

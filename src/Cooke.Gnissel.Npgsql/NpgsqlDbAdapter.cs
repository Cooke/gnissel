#region

using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Typed.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Npgsql.NameTranslation;

#endregion

namespace Cooke.Gnissel.Npgsql;

public sealed class NpgsqlDbAdapter : IDbAdapter
{
    private static readonly Type[] BuiltInTypes =
    [
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(byte[])
    ];

    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlDbAdapter(NpgsqlDataSource dataSource)
        : this(dataSource, NullLogger.Instance) { }

    public NpgsqlDbAdapter(NpgsqlDataSource dataSource, ILogger logger)
    {
        _dataSource = dataSource;
        TypedSqlGenerator = new NpgsqlTypedSqlGenerator(this);
        Migrator = new NpgsqlMigrator(logger, this);
    }

    public string EscapeIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    public string EscapeIdentifierIfNeeded(string identifier) =>
        Regex.IsMatch(identifier, @"[^a-z0-9_]")
            ? $"\"{identifier.Replace("\"", "\"\"")}\""
            : identifier;

    public DbParameter CreateParameter<TValue>(TValue value, string? dbType) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value, DataTypeName = dbType }
            : new NpgsqlParameter<TValue> { TypedValue = value, DataTypeName = dbType };

    public RenderedSql RenderSql(Sql sql, DbOptions options)
    {
        var sb = new StringBuilder(
            sql.Fragments.Sum(x =>
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
                    parameters.Add(p.ToParameter(options));
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

    public IDbConnector CreateConnector() => new NpgsqlDbConnector(_dataSource);

    public ITypedSqlGenerator TypedSqlGenerator { get; }

    public IMigrator Migrator { get; }

    public bool IsDbMapped(Type type) =>
        type.GetCustomAttribute<DbTypeAttribute>() != null
        || type.IsPrimitive
        || BuiltInTypes.Contains(type);

    public string ToColumnName(ParameterInfo parameterInfo) =>
        ConvertToSnakeCase(parameterInfo.Name);

    public string ToColumnName(PropertyInfo propertyInfo) => ConvertToSnakeCase(propertyInfo.Name);

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

#region

using System.Data.Common;
using System.Reflection;
using System.Text;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;
using Npgsql;
using Npgsql.NameTranslation;

#endregion

namespace Cooke.Gnissel.Npgsql;

public sealed class NpgsqlDbAdapter : IDbAdapter
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlDbAdapter(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public string EscapeIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    public DbParameter CreateParameter<TValue>(TValue value, string? dbType) =>
        typeof(TValue) == typeof(object)
            ? new NpgsqlParameter { Value = value, DataTypeName = dbType }
            : new NpgsqlParameter<TValue> { TypedValue = value, DataTypeName = dbType };

    public CompiledSql CompileSql(Sql sql)
    {
        var sb = new StringBuilder(
            sql.Fragments.Sum(
                x =>
                    x switch
                    {
                        Sql.Literal { Value: var s } => s.Length,
                        Sql.IParameter => 3,
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
            }
        }

        return new CompiledSql(sb.ToString(), parameters.ToArray());
    }

    public DbBatchCommand CreateBatchCommand() => new NpgsqlBatchCommand();

    public DbCommand CreateCommand() => new NpgsqlCommand();

    public IDbAccessFactory CreateAccessFactory() => new NpgsqlDbAccessFactory(_dataSource);

    public string GetColumnName(PropertyInfo propertyInfo) =>
        NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(propertyInfo.Name);
}

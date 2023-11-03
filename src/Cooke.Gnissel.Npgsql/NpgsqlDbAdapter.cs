#region

using System.Data.Common;
using System.Reflection;
using System.Text;
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

    public DbConnection CreateConnection() => _dataSource.CreateConnection();

    public DbCommand CreateManagedConnectionCommand() => _dataSource.CreateCommand();

    public (string CommandText, DbParameter[] Parameters) BuildSql(Sql sql)
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

        return ( sb.ToString(),parameters.ToArray());
    }

    public DbBatchCommand CreateBatchCommand() => new NpgsqlBatchCommand();

    public DbCommand CreateCommand() => new NpgsqlCommand();

    public string GetColumnName(PropertyInfo propertyInfo) =>
        NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(propertyInfo.Name);
}

#region

using System.Data.Common;
using Cooke.Gnissel.Typed.Queries;

#endregion

namespace Cooke.Gnissel.Services;

public interface IDbAdapter
{
    string ToColumnName(IEnumerable<string> path);

    string ToTableName(Type type);

    DbParameter CreateParameter<TValue>(TValue value, string? dbType = null);

    RenderedSql RenderSql(Sql sql, DbOptions options);

    DbBatchCommand CreateBatchCommand();

    DbCommand CreateCommand();

    IDbConnector CreateConnector();

    ValueTask Migrate(
        IReadOnlyCollection<Migration> migrations,
        CancellationToken cancellationToken
    );

    bool IsDbMapped(Type type);

    Sql Generate(IInsertQuery query);

    Sql Generate(IDeleteQuery query);

    Sql Generate(IUpdateQuery query);

    Sql Generate(ExpressionQuery query);
}

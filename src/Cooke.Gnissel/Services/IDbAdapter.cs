#region

using System.Data.Common;
using Cooke.Gnissel.Typed.Queries;

#endregion

namespace Cooke.Gnissel.Services;

public interface IDbAdapter
{
    DbParameter CreateParameter<TValue>(TValue value, string? dbType = null);

    RenderedSql RenderSql(Sql sql, DbOptions options);

    DbBatchCommand CreateBatchCommand();

    DbCommand CreateCommand();

    IDbConnector CreateConnector();

    ValueTask Migrate(
        IReadOnlyCollection<Migration> migrations,
        CancellationToken cancellationToken
    );

    Sql Generate(IInsertQuery query);

    Sql Generate(IDeleteQuery query, DbOptions options);

    Sql Generate(IUpdateQuery query, DbOptions options);

    Sql Generate(ExpressionQuery query, DbOptions options);
}

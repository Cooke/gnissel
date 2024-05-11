#region

using System.Data.Common;
using System.Reflection;
using Cooke.Gnissel.Typed.Services;

#endregion

namespace Cooke.Gnissel.Services;

public interface IDbAdapter
{
    string EscapeIdentifier(string identifier);

    string ToColumnName(IEnumerable<ObjectPathPart> path);

    string ToTableName(Type type);

    DbParameter CreateParameter<TValue>(TValue value, string? dbType = null);

    RenderedSql RenderSql(Sql sql, DbOptions options);

    DbBatchCommand CreateBatchCommand();

    DbCommand CreateCommand();

    IDbConnector CreateConnector();

    ITypedSqlGenerator TypedSqlGenerator { get; }

    IMigrator Migrator { get; }

    bool IsDbMapped(Type type);
}

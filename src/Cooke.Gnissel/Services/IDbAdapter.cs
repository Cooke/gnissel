#region

using System.Data.Common;
using System.Reflection;
using Cooke.Gnissel.CommandFactories;

#endregion

namespace Cooke.Gnissel.Services;

public interface IDbAdapter
{
    string EscapeIdentifier(string identifier);

    string GetColumnName(PropertyInfo propertyInfo);

    DbParameter CreateParameter<TValue>(TValue value, string? dbType = null);

    CompiledSql CompileSql(Sql sql);

    DbBatchCommand CreateBatchCommand();
    DbCommand CreateCommand();
    IDbAccessFactory CreateAccessFactory();
}

#region

using System.Data.Common;
using System.Reflection;

#endregion

namespace Cooke.Gnissel.Services;

public interface IDbAdapter
{
    string EscapeIdentifier(string identifier);

    string GetColumnName(PropertyInfo propertyInfo);

    DbParameter CreateParameter<TValue>(TValue value, string? dbType = null);

    DbConnection CreateConnection();

    DbCommand CreateCommand();

    DbCommand CreateManagedConnectionCommand();
    void PopulateCommand(DbCommand cmd, Sql sql);
}

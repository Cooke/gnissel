using System.Data.Common;
using System.Reflection;

namespace Cooke.Gnissel;

public interface IDbAdapter
{
    string EscapeIdentifier(string identifier);

    string GetColumnName(PropertyInfo propertyInfo);

    DbParameter CreateParameter<TValue>(TValue value);

    DbConnection CreateConnection();

    DbCommand CreateCommand();

    DbCommand CreateManagedConnectionCommand();
}

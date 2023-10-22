using System.Data.Common;

namespace Cooke.Gnissel;

public interface DbAdapter
{
    string EscapeIdentifier(string identifier);

    DbParameter CreateParameter<TValue>(TValue value);

    DbCommand CreateCommand();

    DbCommand CreateCommand(DbConnection connection);

    Task<DbConnection> OpenConnection();
}

using System.Data.Common;

namespace Cooke.Gnissel.Services;

public interface IDbConnector
{
    DbCommand CreateCommand();

    DbBatch CreateBatch();

    DbConnection CreateConnection();
}

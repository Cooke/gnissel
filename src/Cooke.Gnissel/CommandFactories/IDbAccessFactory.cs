using System.Data.Common;

namespace Cooke.Gnissel.CommandFactories;

public interface IDbAccessFactory
{
    DbCommand CreateCommand();

    DbBatch CreateBatch();

    DbConnection CreateConnection();
}

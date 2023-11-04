#region

using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

#endregion

namespace Cooke.Gnissel;

public record DbOptions(
    IDbAdapter DbAdapter,
    IRowReader RowReader,
    IQueryExecutor QueryExecutor,
    IDbAccessFactory DbAccessFactory
)
{
    public DbOptions(IDbAdapter dbAdapter)
        : this(dbAdapter, new DefaultRowReader(new DefaultObjectReaderProvider(dbAdapter.DefaultIdentifierMapper))) { }

    public DbOptions(IDbAdapter dbAdapter, IRowReader rowReader)
        : this(
            dbAdapter,
            rowReader,
            new DefaultQueryExecutor(),
            dbAdapter.CreateAccessFactory()
        ) { }
}

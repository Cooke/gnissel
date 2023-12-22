#region

using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

#endregion

namespace Cooke.Gnissel;

public record DbOptions(
    IDbAdapter DbAdapter,
    IObjectReaderProvider ObjectReaderProvider,
    IDbConnector DbConnector,
    IIdentifierMapper IdentifierMapper
)
{
    public DbOptions(IDbAdapter dbAdapter)
        : this(dbAdapter, new DefaultObjectReaderProvider(dbAdapter.DefaultIdentifierMapper)) { }

    public DbOptions(IDbAdapter dbAdapter, IObjectReaderProvider objectReaderProvider)
        : this(
            dbAdapter,
            objectReaderProvider,
            dbAdapter.CreateConnector(),
            dbAdapter.DefaultIdentifierMapper
        ) { }
}

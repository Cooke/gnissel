#region

using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Typed.Services;

#endregion

namespace Cooke.Gnissel;

public record DbOptions(
    IDbAdapter DbAdapter,
    IObjectReaderProvider ObjectReaderProvider,
    IDbConnector DbConnector
)
{
    public DbOptions(IDbAdapter dbAdapter)
        : this(dbAdapter, new DefaultObjectReaderProvider(dbAdapter.IdentifierMapper)) { }

    public DbOptions(IDbAdapter dbAdapter, IObjectReaderProvider objectReaderProvider)
        : this(dbAdapter, objectReaderProvider, dbAdapter.CreateConnector()) { }

    public ITypedSqlGenerator TypedSqlGenerator => DbAdapter.TypedSqlGenerator;
}

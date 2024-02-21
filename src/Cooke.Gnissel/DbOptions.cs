#region

using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Typed.Services;

#endregion

namespace Cooke.Gnissel;

public record DbOptions(
    IDbAdapter DbAdapter,
    IObjectReaderProvider ObjectReaderProvider,
    IDbConnector DbConnector,
    IIdentifierMapper IdentifierMapper,
    ISqlGenerator SqlGenerator
)
{
    public DbOptions(IDbAdapter dbAdapter, ISqlGenerator sqlGenerator)
        : this(
            dbAdapter,
            new DefaultObjectReaderProvider(dbAdapter.DefaultIdentifierMapper),
            sqlGenerator
        ) { }

    public DbOptions(
        IDbAdapter dbAdapter,
        IObjectReaderProvider objectReaderProvider,
        ISqlGenerator sqlGenerator
    )
        : this(
            dbAdapter,
            objectReaderProvider,
            dbAdapter.CreateConnector(),
            dbAdapter.DefaultIdentifierMapper,
            sqlGenerator
        ) { }
}

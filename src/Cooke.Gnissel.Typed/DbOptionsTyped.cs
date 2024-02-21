using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Typed.Services;

namespace Cooke.Gnissel.Typed;

public record DbOptionsTyped(
    IDbAdapter DbAdapter,
    IObjectReaderProvider ObjectReaderProvider,
    IDbConnector DbConnector,
    IIdentifierMapper IdentifierMapper,
    ISqlGenerator SqlGenerator
) : DbOptions(DbAdapter, ObjectReaderProvider, DbConnector, IdentifierMapper)
{
    public DbOptionsTyped(IDbAdapter dbAdapter, ISqlGenerator sqlGenerator)
        : this(
            dbAdapter,
            new DefaultObjectReaderProvider(dbAdapter.DefaultIdentifierMapper),
            sqlGenerator
        ) { }

    public DbOptionsTyped(
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
};

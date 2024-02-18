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
    public DbOptionsTyped(IDbAdapter DbAdapter)
        : this(
            DbAdapter,
            new DefaultObjectReaderProvider(DbAdapter.DefaultIdentifierMapper),
            DbAdapter.CreateConnector(),
            DbAdapter.DefaultIdentifierMapper,
            new SqlGenerator(DbAdapter.DefaultIdentifierMapper)
        ) { }
}

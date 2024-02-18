using Cooke.Gnissel;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using PlusPlusLab.Services;

namespace PlusPlusLab;

public record DbOptionsPlus(
    IDbAdapter DbAdapter,
    IObjectReaderProvider ObjectReaderProvider,
    IDbConnector DbConnector,
    IIdentifierMapper IdentifierMapper,
    ISqlGenerator SqlGenerator
) : DbOptions(DbAdapter, ObjectReaderProvider, DbConnector, IdentifierMapper)
{
    public DbOptionsPlus(IDbAdapter DbAdapter)
        : this(
            DbAdapter,
            new DefaultObjectReaderProvider(DbAdapter.DefaultIdentifierMapper),
            DbAdapter.CreateConnector(),
            DbAdapter.DefaultIdentifierMapper,
            new SqlGenerator(DbAdapter.DefaultIdentifierMapper)
        ) { }
}

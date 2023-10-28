using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;

namespace Cooke.Gnissel;

public record DbOptions(
    IDbAdapter DbAdapter,
    IObjectMapper ObjectMapper,
    IQueryExecutor QueryExecutor,
    ICommandFactory CommandFactory
)
{
    public DbOptions(IDbAdapter dbAdapter)
        : this(dbAdapter, new DefaultObjectMapper(new DefaultObjectMapperValueReader())) { }

    public DbOptions(IDbAdapter dbAdapter, IObjectMapper objectMapper)
        : this(
            dbAdapter,
            objectMapper,
            new DefaultQueryExecutor(),
            new ManagedConnectionCommandFactory(dbAdapter)
        ) { }
}

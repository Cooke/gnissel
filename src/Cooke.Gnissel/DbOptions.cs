namespace Cooke.Gnissel;

public record DbOptions(
    IDbAdapter DbAdapter,
    IObjectMapper ObjectMapper,
    IQueryExecutor QueryExecutor,
    ICommandFactory CommandFactory
)
{
    public DbOptions(IDbAdapter dbAdapter)
        : this(dbAdapter, new ObjectMapper()) { }

    public DbOptions(IDbAdapter dbAdapter, IObjectMapper objectMapper)
        : this(
            dbAdapter,
            objectMapper,
            new QueryExecutor(),
            new ManagedConnectionCommandFactory(dbAdapter)
        ) { }
}

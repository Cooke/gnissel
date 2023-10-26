namespace Cooke.Gnissel;

public class DbOptions
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

    public DbOptions(
        IDbAdapter dbAdapter,
        IObjectMapper objectMapper,
        IQueryExecutor queryExecutor,
        ICommandFactory commandFactory
    )
    {
        ObjectMapper = objectMapper;
        DbAdapter = dbAdapter;
        CommandFactory = commandFactory;
        QueryExecutor = queryExecutor;
    }

    public IObjectMapper ObjectMapper { get; }
    public IDbAdapter DbAdapter { get; }
    public ICommandFactory CommandFactory { get; }
    public IQueryExecutor QueryExecutor { get; }
}

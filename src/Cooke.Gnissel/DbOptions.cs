namespace Cooke.Gnissel;

public class DbOptions
{
    public DbOptions(IDbAdapter dbAdapter)
        : this(new ObjectMapper(), dbAdapter) { }

    public DbOptions(IObjectMapper objectMapper, IDbAdapter dbAdapter)
        : this(objectMapper, dbAdapter, new ReadyCommandProvider(dbAdapter), new QueryExecutor())
    { }

    public DbOptions(
        IObjectMapper objectMapper,
        IDbAdapter dbAdapter,
        ICommandProvider commandProvider,
        IQueryExecutor queryExecutor
    )
    {
        ObjectMapper = objectMapper;
        DbAdapter = dbAdapter;
        CommandProvider = commandProvider;
        QueryExecutor = queryExecutor;
    }

    public IObjectMapper ObjectMapper { get; }
    public IDbAdapter DbAdapter { get; }
    public ICommandProvider CommandProvider { get; }
    public IQueryExecutor QueryExecutor { get; }
}

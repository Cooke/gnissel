namespace Cooke.Gnissel;

public class DbContext
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDbAdapter _dbAdapter;
    private readonly ICommandProvider _commandProvider;
    private readonly IQueryExecutor _queryExecutor;

    public DbContext(DbOptions dbOptions)
    {
        _objectMapper = dbOptions.ObjectMapper;
        _dbAdapter = dbOptions.DbAdapter;
        _commandProvider = dbOptions.CommandProvider;
        _queryExecutor = dbOptions.QueryExecutor;
    }

    public IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        CancellationToken cancellationToken = default
    ) => Query(formattedSql, _objectMapper.Map<TOut>, cancellationToken);

    public IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        Func<RowReader, TOut> mapper,
        CancellationToken cancellationToken = default
    ) =>
        _queryExecutor.Execute(
            formattedSql,
            mapper,
            _commandProvider,
            _dbAdapter,
            cancellationToken
        );

    public async Task Transaction(IEnumerable<IInsertStatement> statements)
    {
        await using var connection = _dbAdapter.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var statement in statements)
        {
            await statement.ExecuteAsync(connection);
        }
        await transaction.CommitAsync();
    }
}

#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel;

public class DbContext
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDbAdapter _dbAdapter;
    private readonly ICommandFactory _commandFactory;
    private readonly IQueryExecutor _queryExecutor;

    public DbContext(DbOptions dbOptions)
    {
        _objectMapper = dbOptions.ObjectMapper;
        _dbAdapter = dbOptions.DbAdapter;
        _commandFactory = dbOptions.CommandFactory;
        _queryExecutor = dbOptions.QueryExecutor;
    }

    internal IDbAdapter Adapter => _dbAdapter;

    public IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        CancellationToken cancellationToken = default
    ) => Query(formattedSql, _objectMapper.Map<TOut>, cancellationToken);

    public IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        Func<DbDataReader, TOut> mapper,
        CancellationToken cancellationToken = default
    ) =>
        _queryExecutor.Execute(
            formattedSql,
            mapper,
            _commandFactory,
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

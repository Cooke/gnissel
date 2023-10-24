using System.Collections.Concurrent;
using System.Data;

namespace Cooke.Gnissel;

public static class DbContextExtensions
{
    public static Task Transaction(this DbContext dbContext, params IInsertStatement[] statements)
    {
        return dbContext.Transaction(statements);
    }
}

public interface IDbContext
{
    IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        CancellationToken cancellationToken = default
    );
}

public abstract class DbContext : IDbContext
{
    private readonly ObjectMapper _objectMapper;
    private readonly DbAdapter _dbAdapter;
    private readonly ICommandProvider _commandProvider;
    private readonly ConcurrentDictionary<Type, object> _tables =
        new ConcurrentDictionary<Type, object>();

    protected DbContext(ObjectMapper objectMapper, DbAdapter dbAdapter)
    {
        _objectMapper = objectMapper;
        _dbAdapter = dbAdapter;
        _commandProvider = new ReadyCommandProvider(dbAdapter);
    }

    protected Table<T> Table<T>() =>
        (Table<T>)
            _tables.GetOrAdd(typeof(T), _ => new Table<T>(_dbAdapter, _commandProvider, _objectMapper));

    public IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        CancellationToken cancellationToken = default
    ) => Query(formattedSql, _objectMapper.Map<TOut>, cancellationToken);

    public IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        Func<Row, TOut> mapper,
        CancellationToken cancellationToken = default
    )
    {
        return QueryExecutor.Execute(formattedSql, mapper, _commandProvider, _dbAdapter, cancellationToken);
    }

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

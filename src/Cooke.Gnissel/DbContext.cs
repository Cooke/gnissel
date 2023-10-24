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

public class DbContextOptions
{
    public DbContextOptions(IDbAdapter dbAdapter)
        : this(new ObjectMapper(), dbAdapter) { }

    public DbContextOptions(IObjectMapper objectMapper, IDbAdapter dbAdapter)
        : this(objectMapper, dbAdapter, new ReadyCommandProvider(dbAdapter)) { }

    public DbContextOptions(
        IObjectMapper objectMapper,
        IDbAdapter dbAdapter,
        ICommandProvider commandProvider
    )
    {
        ObjectMapper = objectMapper;
        DbAdapter = dbAdapter;
        CommandProvider = commandProvider;
    }

    public IObjectMapper ObjectMapper { get; }
    public IDbAdapter DbAdapter { get; }
    public ICommandProvider CommandProvider { get; }
}

public abstract class DbContext : IDbContext
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDbAdapter _dbAdapter;
    private readonly ICommandProvider _commandProvider;

    protected DbContext(DbContextOptions dbContextOptions)
    {
        _objectMapper = dbContextOptions.ObjectMapper;
        _dbAdapter = dbContextOptions.DbAdapter;
        _commandProvider = dbContextOptions.CommandProvider;
    }

    public IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        CancellationToken cancellationToken = default
    ) => Query(formattedSql, _objectMapper.Map<TOut>, cancellationToken);

    public IAsyncEnumerable<TOut> Query<TOut>(
        FormattedSql formattedSql,
        Func<Row, TOut> mapper,
        CancellationToken cancellationToken = default
    ) =>
        QueryExecutor.Execute(
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

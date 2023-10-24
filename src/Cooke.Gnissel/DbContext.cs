using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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
        ParameterizedSql parameterizedSql,
        CancellationToken cancellationToken = default
    );
}

public abstract class DbContext : IDbContext
{
    private readonly DbAdapter _dbAdapter;
    private readonly ICommandProvider _commandProvider;
    private readonly ConcurrentDictionary<Type, object> _tables =
        new ConcurrentDictionary<Type, object>();

    protected DbContext(DbAdapter dbAdapter)
    {
        _dbAdapter = dbAdapter;
        _commandProvider = new ReadyCommandProvider(dbAdapter);
    }

    protected Table<T> Table<T>() =>
        (Table<T>)
            _tables.GetOrAdd(typeof(T), _ => new Table<T>(this, _dbAdapter, _commandProvider));

    public IAsyncEnumerable<TOut> Query<TOut>(
        ParameterizedSql parameterizedSql,
        CancellationToken cancellationToken = default
    )
    {
        // TODO probably a good idea to cache the mappers
        var mapper = CreateTypeMapper<TOut>();
        return Query(parameterizedSql, mapper, cancellationToken);
    }

    private static Func<Row, TOut> CreateTypeMapper<TOut>()
    {
        var ctor = typeof(TOut).GetConstructors().First();
        var row = Expression.Parameter(typeof(Row));
        var mapper = Expression
            .Lambda<Func<Row, TOut>>(
                Expression.New(
                    ctor,
                    ctor.GetParameters()
                        .Select(
                            (p, i) =>
                                Expression.Call(
                                    row,
                                    "Get",
                                    new[] { p.ParameterType },
                                    Expression.Constant(i)
                                )
                        )
                ),
                row
            )
            .Compile();
        return mapper;
    }

    public async IAsyncEnumerable<TOut> Query<TOut>(
        ParameterizedSql parameterizedSql,
        Func<Row, TOut> mapper,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var cmd = _commandProvider.CreateCommand();
        cmd.CommandText = parameterizedSql.Sql;
        foreach (
            var parameter in parameterizedSql.Parameters.Select(x => _dbAdapter.CreateParameter(x))
        )
        {
            cmd.Parameters.Add(parameter);
        }

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return mapper(new Row(reader));
        }
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

public readonly struct Row
{
    private readonly DbDataReader _dataRecord;

    public Row(DbDataReader dataRecord)
    {
        _dataRecord = dataRecord;
    }

    public T Get<T>(string column) => _dataRecord.GetFieldValue<T>(_dataRecord.GetOrdinal(column));

    public T Get<T>(int ordinal) => _dataRecord.GetFieldValue<T>(ordinal);
}

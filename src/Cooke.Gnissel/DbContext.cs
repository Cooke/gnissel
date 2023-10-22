using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public abstract class DbContext
{
    private readonly DbAdapter _dataDbAdapter;
    private readonly ConcurrentDictionary<Type, object> _tables =
        new ConcurrentDictionary<Type, object>();

    protected DbContext(DbAdapter dataDbAdapter)
    {
        _dataDbAdapter = dataDbAdapter;
    }

    protected Table<T> Table<T>() =>
        (Table<T>)_tables.GetOrAdd(typeof(T), _ => new Table<T>(this, _dataDbAdapter));

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
        await using var cmd = _dataDbAdapter.CreateCommand();
        cmd.CommandText = parameterizedSql.Sql;
        foreach (
            var parameter in parameterizedSql.Parameters.Select(
                x => _dataDbAdapter.CreateParameter(x)
            )
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

using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace Cooke.Gnissel;

public abstract class DbContext
{
    private readonly ProviderAdapter _dataProviderAdapter;
    protected DbContext(ProviderAdapter dataProviderAdapter)
    {
        _dataProviderAdapter = dataProviderAdapter;
    }

    protected Table<T> Table<T>() => new Table<T>(_dataProviderAdapter);

    public IAsyncEnumerable<TOut> Query<TOut>(ParameterizedSql parameterizedSql)
    {
        // TODO probably a good idea to cache the mappers
        var mapper = CreateTypeMapper<TOut>();
        return Query(parameterizedSql, mapper);
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
        Func<Row, TOut> mapper
    )
    {
        await using var cmd = _dataProviderAdapter.CreateCommand();
        cmd.CommandText = parameterizedSql.Sql;
        foreach (
            var parameter in parameterizedSql.Parameters.Select(
                x => _dataProviderAdapter.CreateParameter(x)
            )
        )
        {
            cmd.Parameters.Add(parameter);
        }

        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
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

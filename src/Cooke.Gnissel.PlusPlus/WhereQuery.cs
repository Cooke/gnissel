#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Cooke.Gnissel.PlusPlus.Utils;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.PlusPlus;

public class WhereQuery<T1, T2> : IToAsyncEnumerable<ValueTuple<T1, T2>>
{
    private readonly WhereQuery<ValueTuple<T1, T2>> _inner;

    public WhereQuery(
        DbOptions options,
        string table,
        ImmutableArray<Join> joins,
        Expression<Func<T1, T2, bool>>? predicate,
        ImmutableArray<IColumn> columns)
    {
        _inner = new WhereQuery<ValueTuple<T1, T2>>(options, table, joins, predicate?.Let(_ => CreateInnerPredicate(predicate)), columns);
    }
    
    [Pure]
    public ValueTask<(T1, T2)> FirstAsync(
        CancellationToken cancellationToken = default) => _inner.FirstAsync(cancellationToken);
    
    [Pure]
    public ValueTask<(T1, T2)> FirstAsync(Expression<Func<T1, T2, bool>> predicate,
        CancellationToken cancellationToken = default) => _inner.FirstAsync(CreateInnerPredicate(predicate), cancellationToken);

    public IAsyncEnumerable<ValueTuple<T1, T2>> ToAsyncEnumerable() => _inner.ToAsyncEnumerable();
    
    private Expression<Predicate<(T1, T2)>> CreateInnerPredicate(Expression<Func<T1, T2, bool>> predicate)
    {
        var innerParameter = Expression.Parameter(typeof(ValueTuple<T1, T2>));
        var parameterTransformer = new ParameterTransformer([
            (predicate.Parameters[0], Expression.PropertyOrField(innerParameter, "Item1")),
            (predicate.Parameters[1], Expression.PropertyOrField(innerParameter, "Item2"))
        ]);

        var innerBody = parameterTransformer.Visit(predicate.Body);
        var innerCondition = Expression.Lambda<Predicate<ValueTuple<T1, T2>>>(innerBody, innerParameter);
        return innerCondition;
    }

}

public class WhereQuery<T> : IToAsyncEnumerable<T>
{
    private readonly Expression<Predicate<T>>? _condition;
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbConnector _dbConnector;
    private readonly string _table;
    private readonly ImmutableArray<Join> _joins;
    private readonly IObjectReaderProvider _objectReaderProvider;
    private readonly DbOptions _options;

    public WhereQuery(
        DbOptions options,
        string table,
        ImmutableArray<Join> joins,
        Expression<Predicate<T>>? condition,
        ImmutableArray<IColumn> columns
    )
    {
        _options = options;
        _objectReaderProvider = options.ObjectReaderProvider;
        _dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;
        _table = table;
        _joins = joins;
        _condition = condition;
        Columns = columns;
    }

    public ImmutableArray<IColumn> Columns { get; }

    [Pure]
    public WhereQuery<T> Where(Expression<Predicate<T>> predicate) =>
        new(_options, _table, _joins, predicate, Columns);
    
    public async ValueTask<T?> FirstOrDefaultAsync(
        CancellationToken cancellationToken = default
    )
    {
        var query = new Query<T>(
            _dbAdapter.RenderSql(CreateSql<T>(limit: 1)),
            _objectReaderProvider.GetReaderFunc<T>(),
            _dbConnector
        );

        await foreach (var value in query.WithCancellation(cancellationToken))
        {
            return value;
        }

        return default;
    }

    public async ValueTask<T?> FirstOrDefaultAsync(
        Expression<Predicate<T>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        var query = new Query<T>(
            _dbAdapter.RenderSql(Where(predicate).CreateSql<T>(limit: 1)),
            _objectReaderProvider.GetReaderFunc<T>(),
            _dbConnector
        );

        await foreach (var value in query.WithCancellation(cancellationToken))
        {
            return value;
        }

        return default;
    }
    
    public async ValueTask<T> FirstAsync(
        CancellationToken cancellationToken = default
    ) =>
        await FirstOrDefaultAsync(cancellationToken)
        ?? throw new InvalidOperationException("Sequence contains no elements");

    public async ValueTask<T> FirstAsync(
        Expression<Predicate<T>> predicate,
        CancellationToken cancellationToken = default
    ) =>
        await FirstOrDefaultAsync(predicate, cancellationToken)
        ?? throw new InvalidOperationException("Sequence contains no elements");

    [Pure]
    public Query<TOut> Select<TOut>(Expression<Func<T, TOut>> selector) => CreateQuery(selector);

    [Pure]
    private Query<TOut> CreateQuery<TOut>(Expression<Func<T, TOut>> selector) =>
        new(
            _dbAdapter.RenderSql(CreateSql(selector)),
            _objectReaderProvider.GetReaderFunc<TOut>(),
            _dbConnector
        );

    [Pure]
    public IAsyncEnumerable<T> CreateQuery() => CreateQuery<T>(selector: null!);

    private Sql CreateSql<TOut>(Expression<Func<T, TOut>>? selector = null, int? limit = null)
    {
        var sql = new Sql(100, 2);

        sql.AppendLiteral("SELECT ");
        if (selector == null)
        {
            sql.AppendLiteral("*");
        }
        else
        {
            ExpressionRenderer.RenderExpression(
                _options.IdentifierMapper,
                selector.Body,
                selector.Parameters,
                Columns,
                sql
            );
        }

        sql.AppendLiteral(" FROM ");
        sql.AppendIdentifier(_table);
        
        if (_joins.Any())
        {
            foreach (var join in _joins)
            {
                sql.AppendLiteral($" JOIN ");
                sql.AppendIdentifier(join.Table);
                sql.AppendLiteral($" ON ");
                ExpressionRenderer.RenderExpression(
                    _options.IdentifierMapper,
                    join.Condition.Body,
                    join.Condition.Parameters,
                    Columns,
                    sql
                );
            }
        }

        if (_condition != null)
        {
            sql.AppendLiteral(" WHERE ");
            ExpressionRenderer.RenderExpression(
                _options.IdentifierMapper,
                _condition.Body,
                _condition.Parameters,
                Columns,
                sql
            );
        }

        if (limit != null)
        {
            sql.AppendLiteral(" LIMIT ");
            sql.AppendLiteral(limit.Value.ToString());
        }

        return sql;
    }

    public IAsyncEnumerable<T> ToAsyncEnumerable() => CreateQuery();

    
}

public record Join(string Table, LambdaExpression Condition);

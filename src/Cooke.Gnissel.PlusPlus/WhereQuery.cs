#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.PlusPlus;

public class WhereQuery<T> : IToAsyncEnumerable<T>
{
    private readonly Expression<Predicate<T>>? _condition;
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbConnector _dbConnector;
    private readonly string _table;
    private readonly IObjectReaderProvider _objectReaderProvider;
    private readonly DbOptions _options;

    public WhereQuery(
        DbOptions options,
        string table,
        Expression<Predicate<T>>? condition,
        ImmutableArray<Column<T>> columns
    )
    {
        _options = options;
        _objectReaderProvider = options.ObjectReaderProvider;
        _dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;
        _table = table;
        _condition = condition;
        Columns = columns;
    }

    public ImmutableArray<Column<T>> Columns { get; }

    [Pure]
    public WhereQuery<T> Where(Expression<Predicate<T>> predicate) =>
        new(_options, _table, predicate, Columns);

    [Pure]
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

    [Pure]
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
                selector.Parameters.Single(),
                Columns,
                sql
            );
        }

        sql.AppendLiteral($" FROM {_dbAdapter.EscapeIdentifier(_table)}");

        if (_condition != null)
        {
            sql.AppendLiteral(" WHERE ");
            ExpressionRenderer.RenderExpression(
                _options.IdentifierMapper,
                _condition.Body,
                _condition.Parameters.Single(),
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

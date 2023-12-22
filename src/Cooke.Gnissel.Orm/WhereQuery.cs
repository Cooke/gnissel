#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Orm;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.Statements;

public class WhereQuery<T> : IAsyncEnumerable<T>
{
    private readonly string? _condition;
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbConnector _dbConnector;
    private readonly string _table;
    private readonly IObjectReaderProvider _objectReaderProvider;
    private readonly SelectQuery<T> _selectQuery;
    private readonly DbOptions _options;

    public WhereQuery(
        DbOptions options,
        string table,
        string? condition,
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
        _selectQuery = new SelectQuery<T>(options, table, new[] { "*" });
    }

    public ImmutableArray<Column<T>> Columns { get; init; }

    [Pure]
    public WhereQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(
            _options,
            _table,
            ExpressionRenderer.RenderExpression(predicate.Body, predicate.Parameters[0], Columns),
            Columns
        );

    [Pure]
    public SelectQuery<TOut> Select<TOut>(Expression<Func<T, TOut>> selector) =>
        new(
            _options,
            _table,
            new[]
            {
                ExpressionRenderer.RenderExpression(
                    selector.Body,
                    selector.Parameters.Single(),
                    Columns
                )
            }
        );

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var sql = _selectQuery.CreateSql();
        if (_condition != null)
        {
            sql.AppendLiteral(" WHERE ");
            sql.AppendLiteral(_condition);
        }

        var objectReader = _objectReaderProvider.Get<T>();
        return new QueryStatement<T>(
            _dbAdapter.RenderSql(sql),
            (reader, ct) => reader.ReadRows(objectReader, ct),
            _dbConnector
        );
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new()) =>
        ExecuteAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
}

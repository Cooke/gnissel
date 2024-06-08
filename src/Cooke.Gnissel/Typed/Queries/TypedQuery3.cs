using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T1, T2, T3>(ExpressionQuery expressionQuery) : IQuery<(T1, T2, T3)>
{
    private Query<(T1, T2, T3)>? _query;
    private Query<(T1, T2, T3)> LazyQuery => _query ??= expressionQuery.ToQuery<(T1, T2, T3)>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerable<(T1, T2, T3)> ExecuteAsync(
        CancellationToken cancellationToken = default
    ) => LazyQuery.ExecuteAsync(cancellationToken);

    public TypedQuery<T1, T2, T3> Where(Expression<Func<T1, T2, T3, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public OrderByQuery<T1, T2, T3> OrderBy<TProp>(
        Expression<Func<T1, T2, T3, TProp>> propSelector
    ) => new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T1, T2, T3> OrderByDesc<TProp>(
        Expression<Func<T1, T2, T3, TProp>> propSelector
    ) => new(expressionQuery.OrderByDesc(propSelector));

    public GroupByQuery<T1, T2, T3> GroupBy<TProp>(
        Expression<Func<T1, T2, T3, TProp>> propSelector
    ) => new(expressionQuery.GroupBy(propSelector));

    public TypedQuery<T1, T2, T3> Limit(int limit) => new(expressionQuery with { Limit = limit });
}

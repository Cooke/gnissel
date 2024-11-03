using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class OrderByQuery<T1, T2, T3, T4>(ExpressionQuery expressionQuery)
    : IQuery<(T1, T2, T3, T4)>
{
    private Query<(T1, T2, T3, T4)>? _query;
    private Query<(T1, T2, T3, T4)> LazyQuery =>
        _query ??= expressionQuery.ToQuery<(T1, T2, T3, T4)>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerable<(T1, T2, T3, T4)> ExecuteAsync(
        CancellationToken cancellationToken = default
    ) => LazyQuery.ExecuteAsync(cancellationToken);

    public OrderByQuery<T1, T2, T3, T4> ThenBy<TProp>(
        Expression<Func<T1, T2, T3, T4, TProp>> propSelector
    ) => new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T1, T2, T3, T4> ThenByDesc<TProp>(
        Expression<Func<T1, T2, T3, T4, TProp>> propSelector
    ) => new(expressionQuery.OrderByDesc(propSelector));

    public TypedQuery<T1, T2, T3, T4> Limit(int limit) =>
        new(expressionQuery with { Limit = limit });

    public SelectQuery<TSelect> Select<TSelect>(
        Expression<Func<T1, T2, T3, T4, TSelect>> selector
    ) => new(expressionQuery.Select(selector));
}

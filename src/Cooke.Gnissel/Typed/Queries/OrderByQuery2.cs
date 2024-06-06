using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class OrderByQuery<T1, T2>(ExpressionQuery expressionQuery) : IQuery<(T1, T2)>
{
    private Query<(T1, T2)>? _query;
    private Query<(T1, T2)> LazyQuery => _query ??= expressionQuery.ToQuery<(T1, T2)>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerator<(T1, T2)> GetAsyncEnumerator(
        CancellationToken cancellationToken = new()
    ) => LazyQuery.GetAsyncEnumerator(cancellationToken);

    public OrderByQuery<T1, T2> ThenBy<TProp>(Expression<Func<T1, T2, TProp>> propSelector) =>
        new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T1, T2> ThenByDesc<TProp>(Expression<Func<T1, T2, TProp>> propSelector) =>
        new(expressionQuery.OrderByDesc(propSelector));

    public TypedQuery<T1, T2> Limit(int limit) => new(expressionQuery with { Limit = limit });
}

using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class GroupByQuery<T1, T2, T3>(ExpressionQuery expressionQuery)
    : IEnumerableQuery<(T1, T2, T3)>
{
    private Query<(T1, T2, T3)>? _query;
    private Query<(T1, T2, T3)> LazyQuery => _query ??= expressionQuery.ToQuery<(T1, T2, T3)>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerator<(T1, T2, T3)> GetAsyncEnumerator(
        CancellationToken cancellationToken = new()
    ) => LazyQuery.GetAsyncEnumerator(cancellationToken);

    public GroupByQuery<T1, T2, T3> ThenBy<TProp>(
        Expression<Func<T1, T2, T3, TProp>> propSelector
    ) => new(expressionQuery.GroupBy(propSelector));

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T1, T2, T3, TSelect>> selector) =>
        new(expressionQuery.Select(selector));
}

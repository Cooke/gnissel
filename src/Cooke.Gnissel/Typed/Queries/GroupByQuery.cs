using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class GroupByQuery<T>(ExpressionQuery expressionQuery) : IEnumerableQuery<T>
{
    private Query<T>? _query;
    private Query<T> LazyQuery => _query ??= expressionQuery.ToQuery<T>();

    public RenderedSql RenderedSql => LazyQuery.RenderedSql;

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new()) =>
        LazyQuery.GetAsyncEnumerator(cancellationToken);

    public GroupByQuery<T> ThenBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.GroupBy(propSelector));

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        new(expressionQuery.Select(selector));
}

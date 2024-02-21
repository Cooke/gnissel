using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class GroupByQuery<T>(ExpressionQuery expressionQuery) : IToQuery<T>
{
    public Query<T> ToQuery() => expressionQuery.ToQuery<T>();

    public GroupByQuery<T> ThenBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(expressionQuery.GroupBy(propSelector));

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        new(expressionQuery.Select(selector));
}

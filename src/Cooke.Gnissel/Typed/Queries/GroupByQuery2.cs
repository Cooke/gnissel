using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class GroupByQuery<T1, T2>(ExpressionQuery expressionQuery) : IToQuery<(T1, T2)>
{
    public Query<(T1, T2)> ToQuery() => expressionQuery.ToQuery<(T1, T2)>();

    public GroupByQuery<T1, T2> ThenBy<TProp>(Expression<Func<T1, T2, TProp>> propSelector) =>
        new(expressionQuery.GroupBy(propSelector));

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T1, T2, TSelect>> selector) =>
        new(expressionQuery.Select(selector));
}
using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class GroupByQuery<T1, T2, T3>(ExpressionQuery expressionQuery) : IToQuery<(T1, T2, T3)>
{
    public Query<(T1, T2, T3)> ToQuery() => expressionQuery.ToQuery<(T1, T2, T3)>();

    public GroupByQuery<T1, T2, T3> ThenBy<TProp>(Expression<Func<T1, T2, T3, TProp>> propSelector) =>
        new(expressionQuery.GroupBy(propSelector));

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T1, T2, T3, TSelect>> selector) =>
        new(expressionQuery.Select(selector));
}
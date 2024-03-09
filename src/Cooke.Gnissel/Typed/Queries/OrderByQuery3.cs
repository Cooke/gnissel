using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class OrderByQuery<T1, T2, T3>(ExpressionQuery expressionQuery) : IToQuery<(T1, T2, T3)>
{
    public Query<(T1, T2, T3)> ToQuery() => expressionQuery.ToQuery<(T1, T2, T3)>();

    public OrderByQuery<T1, T2, T3> ThenBy<TProp>(Expression<Func<T1, T2, T3, TProp>> propSelector) =>
        new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T1, T2, T3> ThenByDesc<TProp>(Expression<Func<T1, T2, T3, TProp>> propSelector) =>
        new(expressionQuery.OrderByDesc(propSelector));
    
    public TypedQuery<T1, T2, T3> Limit(int limit) 
        => new(expressionQuery with { Limit = limit });
}
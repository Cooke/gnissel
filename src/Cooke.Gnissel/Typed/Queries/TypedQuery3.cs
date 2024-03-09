using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T1, T2, T3>(ExpressionQuery expressionQuery) : IToQuery<(T1, T2, T3)>
{
    public TypedQuery<T1, T2, T3> Where(Expression<Func<T1, T2, T3, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public Query<(T1, T2, T3)> ToQuery() => expressionQuery.ToQuery<(T1, T2, T3)>();
    
    public OrderByQuery<T1, T2, T3> OrderBy<TProp>(Expression<Func<T1, T2, T3, TProp>> propSelector) 
        => new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T1, T2, T3> OrderByDesc<TProp>(Expression<Func<T1, T2, T3, TProp>> propSelector) 
        => new(expressionQuery.OrderByDesc(propSelector));

    public GroupByQuery<T1, T2, T3> GroupBy<TProp>(Expression<Func<T1, T2, T3, TProp>> propSelector) 
        => new(expressionQuery.GroupBy(propSelector));
    
    public TypedQuery<T1, T2, T3> Limit(int limit) 
        => new(expressionQuery with { Limit = limit });
}

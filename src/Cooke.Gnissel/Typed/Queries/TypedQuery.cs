using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T>(ExpressionQuery expressionQuery) : IToQuery<T>
{
    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public Query<T> ToQuery() => expressionQuery.ToQuery<T>();

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        new(expressionQuery.Select(selector));
    
    public FirstQuery<T> First() => new(expressionQuery);
    
    public FirstOrDefaultQuery<T> FirstOrDefault(Expression<Func<T, bool>> predicate) 
        => new (expressionQuery.Where(predicate));
    
    public OrderByQuery<T> OrderBy<TProp>(Expression<Func<T, TProp>> propSelector) 
        => new(expressionQuery.OrderBy(propSelector));

    public OrderByQuery<T> OrderByDesc<TProp>(Expression<Func<T, TProp>> propSelector) 
        => new(expressionQuery.OrderByDesc(propSelector));

    public GroupByQuery<T> GroupBy<TProp>(Expression<Func<T, TProp>> propSelector) 
        => new(expressionQuery.GroupBy(propSelector));
    
    public TypedQuery<T> Limit(int limit) 
        => new(expressionQuery with { Limit = limit });
}

using System.Linq.Expressions;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class TypedQuery<T1, T2>(ExpressionQuery expressionQuery) : IToQuery<(T1, T2)>
{
    public TypedQuery<T1, T2> Where(Expression<Func<T1, T2, bool>> predicate) =>
        new(expressionQuery.Where(predicate));

    public Query<(T1, T2)> ToQuery() => expressionQuery.ToQuery<(T1, T2)>();

    public TypedQuery<T1, T2, T3> Join<T3>(
        Table<T3> joinTable,
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => new(expressionQuery.Join(joinTable, predicate));
    
    public TypedQuery<T1, T2, T3?> LeftJoin<T3>(Table<T3> joinTable, Expression<Func<T1, T2, T3, bool>> predicate)
        => new(expressionQuery.LeftJoin(joinTable, predicate));
    
    public TypedQuery<T1?, T2?, T3> RightJoin<T3>(Table<T3> joinTable, Expression<Func<T1, T2, T3, bool>> predicate)
        => new(expressionQuery.RightJoin(joinTable, predicate));
    
    public TypedQuery<T1?, T2?, T3?> FullJoin<T3>(Table<T3> joinTable, Expression<Func<T1, T2, T3, bool>> predicate)
        => new(expressionQuery.FullJoin(joinTable, predicate));
    
    public TypedQuery<T1, T2, T3> CrossJoin<T3>(Table<T3> joinTable)
        => new(expressionQuery.CrossJoin(joinTable));

    public FirstQuery<(T1, T2)> First() => new(expressionQuery);

    public FirstQuery<(T1, T2)> First(Expression<Func<T1, T2, bool>> predicate) =>
        new(expressionQuery.Where(predicate));
    
    public TypedQuery<T1, T2> Limit(int limit) 
        => new(expressionQuery with { Limit = limit });
    
    
}

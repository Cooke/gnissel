using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed.Querying;

public class TypedQuery<T1, T2>(DbOptionsTyped options, ExpressionQuery expressionQuery)
    : IToQuery<(T1, T2)>
{
    public TypedQuery<T1, T2> Where(Expression<Func<T1, T2, bool>> predicate) =>
        new(options, expressionQuery.Where(predicate));

    public Query<(T1, T2)> ToQuery() => expressionQuery.CreateQuery<(T1, T2)>(options);

    public TypedQuery<T1, T2, T3> Join<T3>(
        Table<T3> joinTable,
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => new(options, expressionQuery.Join(joinTable, predicate));

    public FirstQuery<(T1, T2)> First() => new(options, expressionQuery);

    public FirstQuery<(T1, T2)> First(Expression<Func<T1, T2, bool>> predicate) =>
        new(options, expressionQuery.Where(predicate));
}

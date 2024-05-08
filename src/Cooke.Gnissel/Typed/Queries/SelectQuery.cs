using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class SelectQuery<T>(ExpressionQuery expressionQuery) : IToQuery<T>
{
    public SingleQuery<T> First() => expressionQuery.First<T>();

    public SingleOrDefaultQuery<T> FirstOrDefault() => expressionQuery.FirstOrDefault<T>();

    public Query<T> ToQuery() => expressionQuery.ToQuery<T>();
}

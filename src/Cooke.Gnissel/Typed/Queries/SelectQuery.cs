using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public class SelectQuery<T>(ExpressionQuery expressionQuery) : IToQuery<T>
{
    public FirstQuery<T> First() => new(expressionQuery);

    public FirstOrDefaultQuery<T> FirstOrDefault() => new(expressionQuery);

    public Query<T> ToQuery() => expressionQuery.ToQuery<T>();
}

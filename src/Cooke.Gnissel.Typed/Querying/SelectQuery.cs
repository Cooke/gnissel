using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Querying;

public class SelectQuery<T>(DbOptionsTyped options, ExpressionQuery expressionQuery) : IToQuery<T>
{
    public FirstQuery<T> First() => new(options, expressionQuery);

    public FirstOrDefaultQuery<T> FirstOrDefault() => new(options, expressionQuery);

    public Query<T> ToQuery() => expressionQuery.ToQuery<T>(options);
}

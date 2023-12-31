using System.Linq.Expressions;

namespace Cooke.Gnissel.PlusPlus;

internal class SetCalls<T> : ISetCalls<T>
{
    private readonly List<(LambdaExpression property, LambdaExpression value)> _calls = new();

    public List<(LambdaExpression property, LambdaExpression value)> Calls => _calls;

    public ISetCalls<T> Set<TProperty>(Expression<Func<T, TProperty>> property, TProperty value)
    {
        _calls.Add(
            (
                property,
                Expression.Lambda<Func<T, TProperty>>(
                    Expression.Constant(value),
                    Expression.Parameter(typeof(T))
                )
            )
        );
        return this;
    }

    public ISetCalls<T> Set<TProperty>(
        Expression<Func<T, TProperty>> property,
        Expression<Func<T, TProperty>> value
    )
    {
        _calls.Add((property, value));
        return this;
    }
}

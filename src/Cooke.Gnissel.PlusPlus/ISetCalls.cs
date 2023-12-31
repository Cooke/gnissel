using System.Linq.Expressions;

namespace Cooke.Gnissel.PlusPlus;

public interface ISetCalls<T>
{
    ISetCalls<T> Set<TProperty>(Expression<Func<T, TProperty>> property, TProperty value);

    ISetCalls<T> Set<TProperty>(
        Expression<Func<T, TProperty>> property,
        Expression<Func<T, TProperty>> value
    );
}
using System.Linq.Expressions;

namespace PlusPlusLab;

public interface ISetCollector<T>
{
    ISetCollector<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        TProperty value
    );

    ISetCollector<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        Expression<Func<T, TProperty>> value
    );
}

using System.Linq.Expressions;
using Cooke.Gnissel.Typed.Querying;

namespace Cooke.Gnissel.Typed.Internals;

internal class SetCollector<T>(Table<T> table) : ISetCollector<T>
{
    public List<Setter> Setters { get; } = [];

    public ISetCollector<T> Set<TProperty>(Expression<Func<T, TProperty>> propertySelector, TProperty value)
    {
        Setters.Add(
            new(
                FindColumn(propertySelector),
                
                Expression.Constant(value)
                
            )
        );
        return this;
    }

    public ISetCollector<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        Expression<Func<T, TProperty>> value
    )
    {
        Setters.Add(new(FindColumn(propertySelector), ParameterExpressionReplacer.Replace(value.Body, [
            (value.Parameters.Single(), new TableExpression(new TableSource(table)))
        ])));
        return this;
    }

    private Column<T> FindColumn<TProperty>(Expression<Func<T, TProperty>> columnSelector)
    {
        if (!(columnSelector.Body is MemberExpression { Member: { } memberInfo }))
        {
            throw new ArgumentException("Expected a member expression", nameof(columnSelector));
        }

        var column = table.Columns.FirstOrDefault(x => x.Member == memberInfo) ?? throw new ArgumentException("Column not found", nameof(columnSelector));
        return column;
    }
    
}
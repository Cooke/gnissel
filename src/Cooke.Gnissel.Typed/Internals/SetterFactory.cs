using System.Linq.Expressions;
using Cooke.Gnissel.Typed.Querying;

namespace Cooke.Gnissel.Typed.Internals;

internal static class SetterFactory
{
    public static Setter CreateSetter<TTable,TProperty>(Table<TTable> table, Expression<Func<TTable, TProperty>> propertySelector, TProperty value) 
        => new(FindColumn(table, propertySelector), Expression.Constant(value));

    public static Setter CreateSetter<TTable, TProperty>(Table<TTable> table, Expression<Func<TTable, TProperty>> propertySelector, Expression<Func<TTable, TProperty>> value) 
        => new(FindColumn(table, propertySelector), ParameterExpressionReplacer.Replace(value.Body, [
            (value.Parameters.Single(), new TableExpression(new TableSource(table)))
        ]));

    private static Column<TTable> FindColumn<TTable, TProperty>(
        Table<TTable> table,
        Expression<Func<TTable, TProperty>> columnSelector
    )
    {
        if (!(columnSelector.Body is MemberExpression { Member: { } memberInfo }))
        {
            throw new ArgumentException("Expected a member expression", nameof(columnSelector));
        }

        return table.Columns.FirstOrDefault(x => x.Member == memberInfo)
            ?? throw new ArgumentException("Column not found", nameof(columnSelector));
    }
}

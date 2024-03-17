using System.Linq.Expressions;
using Cooke.Gnissel.Typed.Queries;

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
    ) =>
        table.Columns.FirstOrDefault(x => x.MemberChain.SequenceEqual(ExpressionUtils.GetMemberChain(columnSelector.Body)))
        ?? throw new ArgumentException("Column not found", nameof(columnSelector));
}

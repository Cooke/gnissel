using System.Linq.Expressions;

namespace PlusPlusLab.Internals;

internal class ParameterExpressionReplacer(
    IReadOnlyCollection<(ParameterExpression, Expression)> transformations
) : ExpressionVisitor
{
    public static Expression Replace(Expression expression, IReadOnlyCollection<(ParameterExpression, Expression)> transformations) =>
        new ParameterExpressionReplacer(transformations).Visit(expression);
    
    protected override Expression VisitParameter(ParameterExpression node) =>
        transformations.Where(t => t.Item1 == node).Select(t => t.Item2).FirstOrDefault()
        ?? base.VisitParameter(node);
}
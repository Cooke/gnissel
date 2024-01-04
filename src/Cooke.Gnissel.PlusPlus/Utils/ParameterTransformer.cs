using System.Linq.Expressions;

namespace Cooke.Gnissel.PlusPlus.Utils;

internal class ParameterTransformer(
    IReadOnlyCollection<(ParameterExpression, Expression)> transformations
) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node) =>
        transformations.Where(t => t.Item1 == node).Select(t => t.Item2).FirstOrDefault()
        ?? base.VisitParameter(node);
}

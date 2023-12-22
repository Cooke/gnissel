using System.Linq.Expressions;
using System.Reflection;

namespace Cooke.Gnissel.Orm;

public static class ExpressionRenderer
{
    public static string RenderExpression(
        Expression expression,
        ParameterExpression parameterExpression,
        IReadOnlyCollection<IColumn> columns
    ) =>
        expression switch
        {
            BinaryExpression binaryExpression
                => $"{RenderExpression(binaryExpression.Left, parameterExpression, columns)} {RenderBinaryOperator(binaryExpression.NodeType)} {RenderExpression(binaryExpression.Right, parameterExpression, columns)}",

            ConstantExpression constExp => RenderConstant(constExp.Value),

            MemberExpression memberExpression
                when memberExpression.Expression == parameterExpression
                => columns.First(x => x.Member == memberExpression.Member).Name,

            MemberExpression
            {
                Expression: ConstantExpression constantExpression,
                Member: FieldInfo field
            }
                => RenderConstant(field.GetValue(constantExpression.Value)),

            _ => throw new NotSupportedException()
        };

    private static string RenderBinaryOperator(ExpressionType expressionType) =>
        expressionType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.Multiply => "*",
            ExpressionType.NotEqual => "<>",
            ExpressionType.Or => "OR",
            ExpressionType.Subtract => "-",
            _ => throw new ArgumentOutOfRangeException(nameof(expressionType), expressionType, null)
        };

    private static string RenderConstant(object? value) =>
        value switch
        {
            string => $"'{value}'",
            _ => value?.ToString() ?? "NULL"
        };
}

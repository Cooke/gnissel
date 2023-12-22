using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Orm;

public static class ExpressionRenderer
{
    public static string RenderExpression(
        IIdentifierMapper identifierMapper,
        Expression expression,
        ParameterExpression parameterExpression,
        IReadOnlyCollection<IColumn> columns
    ) =>
        expression switch
        {
            BinaryExpression binaryExpression
                => $"{RenderExpression(identifierMapper, binaryExpression.Left, parameterExpression, columns)} {RenderBinaryOperator(binaryExpression.NodeType)} {RenderExpression(identifierMapper, binaryExpression.Right, parameterExpression, columns)}",

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

            NewExpression newExpression
                => string.Join(
                    ", ",
                    newExpression.Arguments.Select(
                        (arg, i) =>
                            RenderExpression(identifierMapper, arg, parameterExpression, columns)
                            + " AS "
                            + identifierMapper.ToColumnName(
                                newExpression.Constructor!.GetParameters()[i]
                            )
                    )
                ),

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

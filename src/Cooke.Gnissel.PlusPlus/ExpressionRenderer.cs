using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.PlusPlus;

public static class ExpressionRenderer
{
    public static void RenderExpression(
        IIdentifierMapper identifierMapper,
        Expression expression,
        ParameterExpression parameterExpression,
        IReadOnlyCollection<IColumn> columns,
        Sql sql,
        bool constantsAsParameters = false
    )
    {
        switch (expression)
        {
            case BinaryExpression binaryExpression:
                RenderExpression(
                    identifierMapper,
                    binaryExpression.Left,
                    parameterExpression,
                    columns,
                    sql
                );
                sql.AppendLiteral(" ");
                sql.AppendLiteral(RenderBinaryOperator(binaryExpression.NodeType));
                sql.AppendLiteral(" ");
                RenderExpression(
                    identifierMapper,
                    binaryExpression.Right,
                    parameterExpression,
                    columns,
                    sql
                );
                return;

            case ConstantExpression constExp:
                if (constantsAsParameters)
                {
                    sql.AppendParameter(constExp.Value);
                }
                else
                {
                    sql.AppendLiteral(FormatValue(constExp.Value));
                }
                return;

            case MemberExpression memberExpression
                when memberExpression.Expression == parameterExpression:
                sql.AppendLiteral(columns.First(x => x.Member == memberExpression.Member).Name);
                return;

            case MemberExpression
            {
                Expression: ConstantExpression constantExpression,
                Member: FieldInfo field
            }:
                sql.AppendParameter(field.GetValue(constantExpression.Value));
                return;

            case NewExpression newExpression:
                for (var index = 0; index < newExpression.Arguments.Count; index++)
                {
                    var arg = newExpression.Arguments[index];
                    if (index > 0)
                    {
                        sql.AppendLiteral(", ");
                    }

                    RenderExpression(identifierMapper, arg, parameterExpression, columns, sql);
                    sql.AppendLiteral(" AS ");
                    sql.AppendLiteral(
                        identifierMapper.ToColumnName(
                            newExpression.Constructor!.GetParameters()[index]
                        )
                    );
                }

                return;

            default:
                throw new NotSupportedException();
        }
    }

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
            ExpressionType.Add => "+",
            _ => throw new ArgumentOutOfRangeException(nameof(expressionType), expressionType, null)
        };

    private static string FormatValue(object? value) =>
        value switch
        {
            string => $"'{value}'",
            _ => value?.ToString() ?? "NULL"
        };
}

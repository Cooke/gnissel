using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.PlusPlus;

public static class ExpressionRenderer
{
    public static void RenderExpression(
        IIdentifierMapper identifierMapper,
        Expression expression,
        IReadOnlyCollection<(TableSource Source, string? Alias)> sources,
        Sql sql,
        bool constantsAsParameters = false
    )
    {
        switch (expression)
        {
            case BinaryExpression binaryExpression:
                RenderExpression(identifierMapper, binaryExpression.Left, sources, sql);
                sql.AppendLiteral(" ");
                sql.AppendLiteral(RenderBinaryOperator(binaryExpression.NodeType));
                sql.AppendLiteral(" ");
                RenderExpression(identifierMapper, binaryExpression.Right, sources, sql);
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

            case MemberExpression
            {
                Expression: ConstantExpression constantExpression,
                Member: PropertyInfo or FieldInfo
            } memberExpression:
                sql.AppendParameter(GetValue(memberExpression.Member, constantExpression.Value));
                return;

            case MemberExpression memberExpression:
                var source = sources.First(x => x.Source.Expression == memberExpression);
                var column = source.Source.Table.Columns.First(
                    x => x.Member == memberExpression.Member
                );
                if (
                    sources.Any(
                        x => x != source && x.Source.Table.Columns.Any(c => c.Name == column.Name)
                    )
                )
                {
                    sql.AppendIdentifier(source.Alias ?? source.Source.Table.Name);
                    sql.AppendLiteral(".");
                }

                sql.AppendIdentifier(column.Name);
                return;

            case NewExpression newExpression:
                for (var index = 0; index < newExpression.Arguments.Count; index++)
                {
                    var arg = newExpression.Arguments[index];
                    if (index > 0)
                    {
                        sql.AppendLiteral(", ");
                    }

                    RenderExpression(identifierMapper, arg, sources, sql);
                    sql.AppendLiteral(" AS ");
                    sql.AppendLiteral(
                        identifierMapper.ToColumnName(
                            newExpression.Constructor!.GetParameters()[index]
                        )
                    );
                }

                return;

            default:
                throw new NotSupportedException(
                    $"Expression of type {expression.NodeType} not supported"
                );
        }
    }

    private static object? GetValue(MemberInfo memberInfo, object? instance)
    {
        return memberInfo is PropertyInfo p
            ? p.GetValue(instance)
            : memberInfo is FieldInfo f
                ? f.GetValue(instance)
                : throw new InvalidOperationException();
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
            ExpressionType.And => "AND",
            ExpressionType.AndAlso => "AND",
            _ => throw new ArgumentOutOfRangeException(nameof(expressionType), expressionType, null)
        };

    private static string FormatValue(object? value) =>
        value switch
        {
            string => $"'{value}'",
            _ => value?.ToString() ?? "NULL"
        };
}

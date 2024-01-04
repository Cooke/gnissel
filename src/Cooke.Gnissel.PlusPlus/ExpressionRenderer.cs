using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.PlusPlus;

public static class ExpressionRenderer
{
    public static void RenderExpression(
        IIdentifierMapper identifierMapper,
        LambdaExpression expression,
        IReadOnlyCollection<IColumn> columns,
        Sql sql,
        bool constantsAsParameters = false
    )
    {
        RenderExpression(
            identifierMapper,
            expression.Body,
            expression.Parameters,
            columns,
            sql,
            constantsAsParameters
        );
    }

    public static void RenderExpression(
        IIdentifierMapper identifierMapper,
        Expression expression,
        IReadOnlyCollection<ParameterExpression> parameterExpressions,
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
                    parameterExpressions,
                    columns,
                    sql
                );
                sql.AppendLiteral(" ");
                sql.AppendLiteral(RenderBinaryOperator(binaryExpression.NodeType));
                sql.AppendLiteral(" ");
                RenderExpression(
                    identifierMapper,
                    binaryExpression.Right,
                    parameterExpressions,
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

            case MemberExpression
            {
                Expression: ConstantExpression constantExpression,
                Member: PropertyInfo or FieldInfo
            } memberExpression:
                sql.AppendParameter(GetValue(memberExpression.Member, constantExpression.Value));
                return;

            // Support one level of nesting in tuples
            case MemberExpression
            {
                Expression: MemberExpression { Expression: ParameterExpression }
            } innerMemberExpression:

                AppendColumn(columns, sql, innerMemberExpression.Member);
                return;

            case MemberExpression memberExpression:

                AppendColumn(columns, sql, memberExpression.Member);
                return;

            case NewExpression newExpression:
                for (var index = 0; index < newExpression.Arguments.Count; index++)
                {
                    var arg = newExpression.Arguments[index];
                    if (index > 0)
                    {
                        sql.AppendLiteral(", ");
                    }

                    RenderExpression(identifierMapper, arg, parameterExpressions, columns, sql);
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

    private static void AppendColumn(
        IReadOnlyCollection<IColumn> columns,
        Sql sql,
        MemberInfo memberInfo
    )
    {
        var column = columns.First(x => x.Member == memberInfo);
        if (columns.Any(x => x.Name == column.Name && x != column))
        {
            sql.AppendIdentifier(column.Table.Name);
            sql.AppendLiteral(".");
        }

        sql.AppendIdentifier(column.Name);
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

using System.Linq.Expressions;
using Cooke.Gnissel.Typed;
using Cooke.Gnissel.Typed.Internals;
using Cooke.Gnissel.Typed.Queries;

namespace Cooke.Gnissel.Npgsql;

public partial class NpgsqlDbAdapter
{
    public Sql Generate(IInsertQuery query)
    {
        var sql = new Sql(20 + query.Columns.Count * 4);
        sql.AppendLiteral("INSERT INTO ");
        sql.AppendIdentifier(query.Table.Name);
        sql.AppendLiteral(" (");
        var firstColumn = true;
        foreach (var column in query.Columns)
        {
            if (!firstColumn)
                sql.AppendLiteral(", ");
            sql.AppendIdentifier(column.Name);
            firstColumn = false;
        }

        sql.AppendLiteral(") VALUES ");
        bool firstRow = true;
        foreach (var row in query.Rows)
        {
            if (!firstRow)
            {
                sql.AppendLiteral(", ");
            }

            firstRow = false;

            sql.AppendLiteral("(");
            var firstParam = true;
            foreach (var par in row.Parameters)
            {
                if (!firstParam)
                {
                    sql.AppendLiteral(", ");
                }

                sql.AppendDbParameter(par);
                firstParam = false;
            }

            sql.AppendLiteral(")");
        }

        return sql;
    }

    public Sql Generate(IDeleteQuery query)
    {
        var sql = new Sql(100, 2);
        sql.AppendLiteral($"DELETE FROM ");
        sql.AppendIdentifier(query.Table.Name);

        if (query.Condition != null)
        {
            sql.AppendLiteral(" WHERE ");
            RenderExpression(query.Condition, sql, new RenderOptions());
        }

        return sql;
    }

    public Sql Generate(IUpdateQuery query)
    {
        var sql = new Sql(100, 2);
        sql.AppendLiteral("UPDATE ");
        sql.AppendIdentifier(query.Table.Name);

        sql.AppendLiteral(" SET ");

        var first = true;
        foreach (var setter in query.Setters)
        {
            if (!first)
            {
                sql.AppendLiteral(", ");
            }

            sql.AppendIdentifier(setter.Column.Name);
            sql.AppendLiteral(" = ");

            switch (setter)
            {
                case ExpressionSetter expressionSetter:
                    RenderExpression(
                        expressionSetter.Value,
                        sql,
                        new RenderOptions() { ConstantsAsParameters = true }
                    );
                    break;
                case ValueSetter valueSetter:
                    valueSetter.AppendParameter(sql);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(setter));
            }

            first = false;
        }

        if (query.Condition != null)
        {
            sql.AppendLiteral(" WHERE ");
            RenderExpression(query.Condition, sql, new RenderOptions());
        }

        return sql;
    }

    public Sql Generate(ExpressionQuery query)
    {
        var tableSource = query.TableSource;
        var joins = query.Joins;
        var selector = query.Selector;
        var options = new RenderOptions() { QualifyColumns = query.Joins.Any() };

        var sql = new Sql(100, 2);

        sql.AppendLiteral("SELECT ");
        if (selector == null)
        {
            var index = 0;
            foreach (var column in query.TableSource.Table.Columns)
            {
                if (index > 0)
                {
                    sql.AppendLiteral(", ");
                }

                RenderSelectColumn(sql, column, query.TableSource);

                index++;
            }

            // Select joins tables
            foreach (var join in joins)
            {
                foreach (var column in join.TableSource.Table.Columns)
                {
                    sql.AppendLiteral(", ");
                    RenderSelectColumn(sql, column, join.TableSource);
                }
            }
        }
        else
        {
            RenderExpression(selector, sql, options with { });
        }

        sql.AppendLiteral(" FROM ");
        AppendTableSource(tableSource);

        if (joins.Any())
        {
            foreach (var join in joins)
            {
                sql.AppendLiteral(
                    join.Type switch
                    {
                        JoinType.Default => " JOIN ",
                        JoinType.Left => " LEFT JOIN ",
                        JoinType.Right => " RIGHT JOIN ",
                        JoinType.Full => " FULL JOIN ",
                        JoinType.Cross => " CROSS JOIN ",
                        _ => throw new NotSupportedException("Join type not supported"),
                    }
                );
                AppendTableSource(join.TableSource);

                if (join.Condition != null)
                {
                    sql.AppendLiteral($" ON ");
                    RenderExpression(join.Condition, sql, options);
                }
            }
        }

        if (query.Conditions.Any())
        {
            sql.AppendLiteral(" WHERE ");
            bool first = true;
            foreach (var queryCondition in query.Conditions)
            {
                if (!first)
                {
                    sql.AppendLiteral(" AND ");
                }

                RenderExpression(queryCondition, sql, options);
                first = false;
            }
        }

        if (query.Groupings.Any())
        {
            sql.AppendLiteral(" GROUP BY ");
            bool first = true;
            foreach (var groupBy in query.Groupings)
            {
                if (!first)
                {
                    sql.AppendLiteral(", ");
                }
                first = false;

                RenderExpression(groupBy, sql, options);
            }
        }

        if (query.OrderBys.Any())
        {
            sql.AppendLiteral(" ORDER BY ");
            bool first = true;
            foreach (var by in query.OrderBys)
            {
                if (!first)
                {
                    sql.AppendLiteral(", ");
                }
                first = false;

                RenderExpression(by.Expression, sql, options);

                if (by.Descending)
                {
                    sql.AppendLiteral(" DESC");
                }
            }
        }

        if (query.Limit != null)
        {
            sql.AppendLiteral(" LIMIT ");
            sql.AppendLiteral(query.Limit.Value.ToString());
        }

        return sql;

        void AppendTableSource(TableSource source)
        {
            sql.AppendIdentifier(source.Table.Name);
            if (source.Alias != null)
            {
                sql.AppendLiteral($" AS ");
                sql.AppendIdentifier(source.Alias);
            }
        }
    }

    private void RenderSelectColumn(Sql sql, IColumn column, TableSource tableSource)
    {
        sql.AppendIdentifier(tableSource.AliasOrName);
        sql.AppendLiteral(".");
        sql.AppendIdentifier(column.Name);

        var parameterColumnName = column.Name;

        if (column.Name != parameterColumnName)
        {
            sql.AppendLiteral($" AS ");
            sql.AppendIdentifier(parameterColumnName);
        }
    }

    private record RenderOptions
    {
        public bool ConstantsAsParameters { get; init; }

        public bool QualifyColumns { get; init; }
    }

    private void RenderExpression(Expression expression, Sql sql, RenderOptions options)
    {
        switch (expression)
        {
            case BinaryExpression binaryExpression:
                var (left, right) = Coercion(binaryExpression.Left, binaryExpression.Right);
                RenderExpression(left, sql, options);
                sql.AppendLiteral(" ");
                sql.AppendLiteral(RenderBinaryOperator(binaryExpression.NodeType));
                sql.AppendLiteral(" ");
                RenderExpression(right, sql, options);
                return;

            case ConstantExpression constExp:
                if (
                    options.ConstantsAsParameters
                    || constExp.Value is not (string or int or float or double or long or bool)
                )
                {
                    sql.AppendParameter(constExp.Type, constExp.Value);
                }
                else
                {
                    sql.AppendLiteralValue(constExp.Value);
                }
                return;

            case MethodCallExpression methodCallExpression:
                if (methodCallExpression.Method.DeclaringType == typeof(Db))
                {
                    sql.AppendLiteral(methodCallExpression.Method.Name.ToUpperInvariant());
                    sql.AppendLiteral("(");
                    if (methodCallExpression.Arguments.Count > 0)
                    {
                        RenderExpression(methodCallExpression.Arguments.Single(), sql, options);
                    }
                    else
                    {
                        sql.AppendLiteral("*");
                    }
                    sql.AppendLiteral(")");
                }
                else if (TryEvaluateExpression(methodCallExpression, out var value))
                {
                    sql.AppendParameter(methodCallExpression.Type, value);
                }
                else
                {
                    throw new NotSupportedException(
                        $"Method {methodCallExpression.Method.Name} not supported"
                    );
                }
                return;

            case MemberExpression { Expression: TableExpression tableExpression } memberExpression:
                var source = tableExpression.TableSource;
                var column = source.Table.Columns.First(x =>
                    x.MemberChain.SequenceEqual([memberExpression.Member])
                );
                if (options.QualifyColumns)
                {
                    sql.AppendIdentifier(source.AliasOrName);
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

                    RenderExpression(arg, sql, options);
                    sql.AppendLiteral(" AS ");
                    sql.AppendLiteral(
                        ToColumnName(
                            [
                                newExpression.Constructor!.GetParameters()[index].Name
                                    ?? throw new InvalidOperationException(),
                            ]
                        )
                    );
                }

                return;

            case MemberExpression memberExpression:
            {
                if (TryEvaluateExpression(memberExpression, out var value))
                {
                    sql.AppendParameter(memberExpression.Type, value);
                }
                else
                {
                    throw new NotSupportedException(
                        $"Member expression with member {memberExpression.Member.Name} not supported"
                    );
                }

                return;
            }

            case UnaryExpression
            {
                NodeType: ExpressionType.Convert,
                Operand.Type.IsEnum: true
            } convertEnumExpression:
                RenderExpression(convertEnumExpression.Operand, sql, options);
                return;

            case UnaryExpression { NodeType: ExpressionType.Convert } convertExpression:
                RenderExpression(convertExpression.Operand, sql, options);
                return;

            default:
                throw new NotSupportedException(
                    $"Expression of type {expression.NodeType} not supported"
                );
        }
    }

    private (Expression left, Expression right) Coercion(Expression left, Expression right)
    {
        var (left1, right1) = CoerceInner(left, right);
        var (right2, left2) = CoerceInner(right1, left1);
        return (left2, right2);

        static (Expression operand1, Expression operand2) CoerceInner(
            Expression operand1,
            Expression operand2
        )
        {
            if (
                operand1
                    is UnaryExpression
                    {
                        NodeType: ExpressionType.Convert,
                        Operand: { Type.IsEnum: true } operand1Value
                    }
                && operand2
                    is ConstantExpression { Type.IsPrimitive: true, Value: { } operand2Value }
            )
            {
                return (
                    operand1Value,
                    Expression.Constant(Enum.ToObject(operand1Value.Type, operand2Value))
                );
            }

            return (operand1, operand2);
        }
    }

    // TODO remove this dynamic code
    private static bool TryEvaluateExpression(Expression methodCallExpression, out object? result)
    {
        try
        {
            var lambda = Expression.Lambda(methodCallExpression);
            var compiled = lambda.Compile();
            result = compiled.DynamicInvoke();
            return true;
        }
        catch
        {
            result = null;
            return false;
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
            ExpressionType.OrElse => "OR",
            ExpressionType.Subtract => "-",
            ExpressionType.Add => "+",
            ExpressionType.And => "AND",
            ExpressionType.AndAlso => "AND",
            _ => throw new ArgumentOutOfRangeException(
                nameof(expressionType),
                expressionType,
                null
            ),
        };
}

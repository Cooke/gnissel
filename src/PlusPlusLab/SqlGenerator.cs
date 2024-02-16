using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel;
using Cooke.Gnissel.Services;

namespace PlusPlusLab;

public class SqlGenerator(IIdentifierMapper identifierMapper) : ISqlGenerator
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

        sql.AppendLiteral(") VALUES(");
        var firstParam = true;
        foreach (var par in query.Parameters)
        {
            if (!firstParam)
                sql.AppendLiteral(", ");
            sql.AppendFormatted(par);
            firstParam = false;
        }

        sql.AppendLiteral(")");
        return sql;
    }
    
    public Sql Generate(ExpressionQuery expressionQuery)
    {
        var tableSource = expressionQuery.Table;
        var joins = expressionQuery.Joins;
        var selector = expressionQuery.Selector;
        
        var sql = new Sql(100, 2);
        // var tableIndices = CreateTableIndicesMap();
        // var tableAlias = tableIndices.ContainsKey(tableSource.Table.Name) ? $"{tableSource.Table.Name}{tableIndices[tableSource.Table.Name]++}" : null;
        // var joinAliases = joins.Select(x => tableIndices.ContainsKey(x.Table.Name) ? $"{x.Table.Name}{tableIndices[x.Table.Name]++}" : null);
        // var tableSourcesWithAliases = ((IReadOnlyCollection<TableSource>)[tableSource, ..joins]).Zip([tableAlias, ..joinAliases]).ToArray();
        
        sql.AppendLiteral("SELECT ");
        if (selector == null)
        {
            sql.AppendLiteral("*");
        }
        else
        {
            RenderExpression(selector,
                sql
            );
        }

        sql.AppendLiteral(" FROM ");
        AppendTableSource(tableSource);

        // if (joins.Any())
        // {
        //     foreach (var (join, alias) in joins.Zip(joinAliases))
        //     {
        //         sql.AppendLiteral($" JOIN ");
        //         AppendTableSource(join, alias);
        //
        //         sql.AppendLiteral($" ON ");
        //         RenderExpression(join.Condition,
        //             tableSourcesWithAliases,
        //             sql
        //         );
        //     }
        // }
        //
        // if (_condition != null)
        // {
        //     sql.AppendLiteral(" WHERE ");
        //     ExpressionRenderer.RenderExpression(
        //         _options.IdentifierMapper,
        //         _condition,
        //         tableSourcesWithAliases,
        //         sql
        //     );
        // }
        //
        // if (limit != null)
        // {
        //     sql.AppendLiteral(" LIMIT ");
        //     sql.AppendLiteral(limit.Value.ToString());
        // }

        return sql;

        void AppendTableSource(TableSource source)
        {
            var table = source.Table.Name;
            sql.AppendIdentifier(table);
            if (tableSource.Alias != null)
            {
                sql.AppendLiteral($" AS ");
                sql.AppendIdentifier(tableSource.Alias);
            }
        }
        
        Dictionary<string, int> CreateTableIndicesMap()
        {
            var tableCount = new Dictionary<string, int> { { tableSource.Table.Name, 1 } };
            foreach (var join in joins)
            {
                tableCount.TryAdd(join.Table.Name, 0);
                tableCount[join.Table.Name]++;
            }
        
            return tableCount.Where(x => x.Value > 1).ToDictionary(x => x.Key, _ => 0);
        }
    }
    
    public void RenderExpression(Expression expression,
        Sql sql,
        bool constantsAsParameters = false
    )
    {
        switch (expression)
        {
            case BinaryExpression binaryExpression:
                RenderExpression(binaryExpression.Left, sql);
                sql.AppendLiteral(" ");
                sql.AppendLiteral(RenderBinaryOperator(binaryExpression.NodeType));
                sql.AppendLiteral(" ");
                RenderExpression(binaryExpression.Right, sql);
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

            case MemberExpression
            {
                Expression: TableExpression tableExpression
            } memberExpression:
                var source = tableExpression.TableSource;
                var column = source.Table.Columns.First(
                    x => x.Member == memberExpression.Member
                );
                // if (
                //     sources.Any(
                //         x => x != source && x.Source.Table.Columns.Any(c => c.Name == column.Name)
                //     )
                // )
                // {
                //     sql.AppendIdentifier(source.Alias ?? source.Source.Table.Name);
                //     sql.AppendLiteral(".");
                // }

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

                        RenderExpression(arg, sql);
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

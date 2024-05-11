using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed.Internals;

namespace Cooke.Gnissel.Typed.Queries;

public interface IDeleteQuery
{
    ITable Table { get; }

    Expression? Condition { get; }
}

public class DeleteQueryWithoutWhere<T>(Table<T> table, DbOptions options)
{
    public ITable Table { get; } = table;

    public DeleteQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(
            table,
            options,
            ParameterExpressionReplacer.Replace(
                predicate.Body,
                [(predicate.Parameters.Single(), new TableExpression(new TableSource(table)))]
            )
        );

    public DeleteQuery<T> WithoutWhere() => new(table, options, null);
}

public class DeleteQuery<T>(Table<T> table, DbOptions options, Expression? condition) : IDeleteQuery
{
    public ITable Table { get; } = table;

    public Expression? Condition { get; } = condition;

    public ValueTaskAwaiter<int> GetAwaiter() => ExecuteAsync().GetAwaiter();

    public ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default) =>
        new NonQuery(
            options.DbConnector,
            options.RenderSql(options.TypedSqlGenerator.Generate(this))
        ).ExecuteAsync(cancellationToken);
}

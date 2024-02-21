using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public interface IDeleteQuery
{
    ITable Table { get; }

    Expression? Condition { get; }
}

public class DeleteQuery<T>(Table<T> table, DbOptions options, Expression? condition)
    : IDeleteQuery
{
    public ITable Table { get; } = table;

    public Expression? Condition { get; } = condition;

    public ValueTaskAwaiter<int> GetAwaiter() => ExecuteAsync().GetAwaiter();

    public ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default) =>
        new NonQuery(
            options.DbConnector,
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(this))
        ).ExecuteAsync(cancellationToken);
}

using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;

namespace PlusPlusLab;

public class DeleteQuery<T>(Table<T> table, DbOptionsPlus options, Expression? condition) : IDeleteQuery
{
    public ITable Table { get; } = table;
    
    public Expression? Condition { get; } = condition;

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var sql = options.SqlGenerator.Generate(this);
        var q = new NonQuery(options.DbConnector, options.DbAdapter.RenderSql(sql), cancellationToken);
        return q.ExecuteAsync();
    }
}
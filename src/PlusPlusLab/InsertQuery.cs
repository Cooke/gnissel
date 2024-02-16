using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;

namespace PlusPlusLab;

public class InsertQuery<T>(Table<T> table, IReadOnlyCollection<Column<T>> columns, DbOptionsPlus options, IReadOnlyCollection<DbParameter> parameters) : IInsertQuery
{
    public ITable Table { get; } = table;
    
    public IReadOnlyCollection<IColumn> Columns { get; } = columns;
    
    public IReadOnlyCollection<DbParameter> Parameters { get; } = parameters;

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var sql = options.SqlGenerator.Generate(this);
        var q = new ExecuteQuery(options.DbConnector, options.DbAdapter.RenderSql(sql), cancellationToken);
        return q.ExecuteAsync();
    }
}
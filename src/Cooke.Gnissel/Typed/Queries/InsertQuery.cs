using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public interface IInsertQuery
{
    ITable Table { get; }

    IReadOnlyCollection<IColumn> Columns { get; }

    IReadOnlyCollection<RowParameters> Rows { get; }
}

public record RowParameters(IReadOnlyCollection<Sql.Parameter> Parameters);

public class InsertQuery<T>(
    Table<T> table,
    IReadOnlyCollection<Column<T>> columns,
    DbOptions options,
    IReadOnlyCollection<RowParameters> rows
) : IInsertQuery, INonQuery
{
    public ITable Table { get; } = table;

    public IReadOnlyCollection<IColumn> Columns { get; } = columns;

    public IReadOnlyCollection<RowParameters> Rows { get; } = rows;

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default) =>
        new NonQuery(
            options.DbConnector,
            options.RenderSql(options.DbAdapter.Generate(this))
        ).ExecuteAsync(cancellationToken);

    public RenderedSql RenderedSql => options.RenderSql(options.DbAdapter.Generate(this));
}

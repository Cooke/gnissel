using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public interface IInsertStatement
{
    Task<int> ExecuteAsync();
    Task<int> ExecuteAsync(DbConnection connection);
}

public record InsertStatement<T>(
    DbAdapter DbAdapter,
    Table<T> Table,
    IEnumerable<IColumn<T>> Columns,
    IEnumerable<DbParameter> Parameters
) : IInsertStatement
{
    public TaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async Task<int> ExecuteAsync()
    {
        await using var command = DbAdapter.CreateCommand();
        return await ExecuteAsync(command);
    }

    public async Task<int> ExecuteAsync(DbConnection connection)
    {
        await using var command = DbAdapter.CreateCommand(connection);
        return await ExecuteAsync(command);
    }

    private async Task<int> ExecuteAsync(DbCommand command)
    {
        var cols = string.Join(", ", Columns.Select(x => DbAdapter.EscapeIdentifier(x.Name)));
        var paramPlaceholders = string.Join(", ", Columns.Select((_, i) => "$" + (i + 1)));

        command.CommandText =
            $"INSERT INTO {DbAdapter.EscapeIdentifier(Table.Name)}({cols}) VALUES({paramPlaceholders})";
        foreach (var dbParameter in Parameters)
        {
            command.Parameters.Add(dbParameter);
        }

        return await command.ExecuteNonQueryAsync();
    }
}

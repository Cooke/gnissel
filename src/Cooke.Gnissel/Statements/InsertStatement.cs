using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public record InsertStatement<T>(
    ProviderAdapter ProviderAdapter,
    Table<T> Table,
    IEnumerable<IColumn<T>> Columns,
    IEnumerable<DbParameter> Parameters
)
{
    public TaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async Task<int> ExecuteAsync()
    {
        var cols = string.Join(", ", Columns.Select(x => x.Name));
        var paramPlaceholders = string.Join(", ", Columns.Select((_, i) => "$" + (i + 1)));

        var command = ProviderAdapter.CreateCommand();
        command.CommandText =
            $"INSERT INTO {ProviderAdapter.EscapeIdentifier(Table.Name)}({cols}) VALUES({paramPlaceholders})";
        foreach (var dbParameter in Parameters)
        {
            command.Parameters.Add(dbParameter);
        }

        return await command.ExecuteNonQueryAsync();
    }
}

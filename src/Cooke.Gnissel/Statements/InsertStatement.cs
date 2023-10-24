using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Cooke.Gnissel;

public interface IInsertStatement
{
    Task<int> ExecuteAsync(DbConnection connection);
}

public class InsertStatement<T> : IInsertStatement
{
    private readonly ICommandProvider _commandProvider;
    private readonly DbAdapter _dbAdapter;
    private readonly Table<T> _table;
    private readonly IEnumerable<DbParameter> _parameters;
    private readonly IEnumerable<IColumn<T>> _columns;

    internal InsertStatement(
        ICommandProvider commandProvider,
        DbAdapter dbAdapter,
        Table<T> table,
        IEnumerable<IColumn<T>> columns,
        IEnumerable<DbParameter> parameters
    )
    {
        _commandProvider = commandProvider;
        _dbAdapter = dbAdapter;
        _table = table;
        _columns = columns;
        _parameters = parameters;
    }

    public TaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async Task<int> ExecuteAsync()
    {
        await using var command = _commandProvider.CreateCommand();
        return await ExecuteAsync(command);
    }

    public async Task<int> ExecuteAsync(DbConnection connection)
    {
        await using var cmd = _dbAdapter.CreateEmptyCommand();
        cmd.Connection = connection;
        return await ExecuteAsync(cmd);
    }

    private async Task<int> ExecuteAsync(DbCommand command)
    {
        var cols = string.Join(", ", _columns.Select(x => _dbAdapter.EscapeIdentifier(x.Name)));
        var paramPlaceholders = string.Join(", ", _columns.Select((_, i) => "$" + (i + 1)));

        command.CommandText =
            $"INSERT INTO {_dbAdapter.EscapeIdentifier(_table.Name)}({cols}) VALUES({paramPlaceholders})";
        foreach (var dbParameter in _parameters)
        {
            command.Parameters.Add(dbParameter);
        }

        return await command.ExecuteNonQueryAsync();
    }
}

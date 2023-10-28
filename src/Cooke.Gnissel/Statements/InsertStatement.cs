using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Statements;

public interface IInsertStatement
{
    Task<int> ExecuteAsync(DbConnection connection);
}

public class InsertStatement<T> : IInsertStatement
{
    private readonly ICommandFactory _commandFactory;
    private readonly IDbAdapter _dbAdapter;
    private readonly Table<T> _table;
    private readonly IEnumerable<DbParameter> _parameters;
    private readonly IEnumerable<Column<T>> _columns;

    internal InsertStatement(
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        Table<T> table,
        IEnumerable<Column<T>> columns,
        IEnumerable<DbParameter> parameters
    )
    {
        _commandFactory = commandFactory;
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
        await using var command = _commandFactory.CreateCommand();
        return await ExecuteAsync(command);
    }

    public async Task<int> ExecuteAsync(DbConnection connection)
    {
        await using var cmd = _dbAdapter.CreateCommand();
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

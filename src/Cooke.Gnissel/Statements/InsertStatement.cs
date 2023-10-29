using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel.Statements;

public class InsertStatement<T> : IExecuteStatement
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

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var command = _commandFactory.CreateCommand();
        return await ExecuteAsync(command, cancellationToken);
    }

    public async ValueTask<int> ExecuteAsync(ICommandFactory? commandFactory, CancellationToken cancellationToken = default)
    {
        await using var cmd = (commandFactory ?? _commandFactory).CreateCommand();
        return await ExecuteAsync(cmd, cancellationToken);
    }

    private async Task<int> ExecuteAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var cols = string.Join(", ", _columns.Select(x => _dbAdapter.EscapeIdentifier(x.Name)));
        var paramPlaceholders = string.Join(", ", _columns.Select((_, i) => "$" + (i + 1)));

        command.CommandText =
            $"INSERT INTO {_dbAdapter.EscapeIdentifier(_table.Name)}({cols}) VALUES({paramPlaceholders})";
        foreach (var dbParameter in _parameters)
        {
            command.Parameters.Add(dbParameter);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

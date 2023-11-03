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
    private readonly Sql _sql;

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

        Sql sql = new Sql(20 + _columns.Count() * 2 + _parameters.Count() * 2);
        sql.AppendLiteral("INSERT INTO ");
        sql.AppendLiteral(_dbAdapter.EscapeIdentifier(_table.Name));
        sql.AppendLiteral(" (");
        var firstColumn = true;
        foreach (var column in _columns)
        {
            if (!firstColumn)
            {
                sql.AppendLiteral(", ");
            }
            sql.AppendLiteral(_dbAdapter.EscapeIdentifier(column.Name));
            firstColumn = false;
        }
        sql.AppendLiteral(") VALUES(");
        bool firstParam = true;
        foreach (var dbParameter in _parameters)
        {
            if (!firstParam)
            {
                sql.AppendLiteral(", ");
            }
            sql.AppendFormatted(dbParameter);
            firstParam = false;
        }
        sql.AppendLiteral(")");
        _sql = sql;
    }

    public Sql Sql => _sql;

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    public async ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var command = _commandFactory.CreateCommand();
        return await ExecuteAsync(command, cancellationToken);
    }

    public async ValueTask<int> ExecuteAsync(
        ICommandFactory? commandFactory,
        CancellationToken cancellationToken = default
    )
    {
        await using var cmd = (commandFactory ?? _commandFactory).CreateCommand();
        return await ExecuteAsync(cmd, cancellationToken);
    }

    private async Task<int> ExecuteAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var (commandText, parameters) = _dbAdapter.BuildSql(Sql);
        command.CommandText = commandText;
        command.Parameters.AddRange(parameters);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Statements;

namespace Cooke.Gnissel.Services.Implementations;

public class ExecuteStatement : IExecuteStatement
{
    private readonly ICommandFactory _commandFactory;
    private readonly CancellationToken _cancellationToken;
    private readonly CompiledSql _compiledSql;

    public ExecuteStatement(
        ICommandFactory commandFactory,
        CompiledSql compiledSql,
        CancellationToken cancellationToken
    )
    {
        _compiledSql = compiledSql;
        _commandFactory = commandFactory;
        _cancellationToken = cancellationToken;
    }

    public CompiledSql CompiledSql => _compiledSql;

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();

        async ValueTask<int> ExecuteAsync()
        {
            await using var cmd = _commandFactory.CreateCommand();
            cmd.CommandText = _compiledSql.CommandText;
            cmd.Parameters.AddRange(_compiledSql.Parameters);
            return await cmd.ExecuteNonQueryAsync(_cancellationToken);
        }
    }
}

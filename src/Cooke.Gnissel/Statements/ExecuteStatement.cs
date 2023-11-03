using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Statements;

namespace Cooke.Gnissel.Services.Implementations;

public class ExecuteStatement
{
    private readonly IDbAccessFactory _dbAccessFactory;
    private readonly CancellationToken _cancellationToken;
    private readonly CompiledSql _compiledSql;

    public ExecuteStatement(
        IDbAccessFactory dbAccessFactory,
        CompiledSql compiledSql,
        CancellationToken cancellationToken
    )
    {
        _compiledSql = compiledSql;
        _dbAccessFactory = dbAccessFactory;
        _cancellationToken = cancellationToken;
    }

    public CompiledSql CompiledSql => _compiledSql;

    public ValueTaskAwaiter<int> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();

        async ValueTask<int> ExecuteAsync()
        {
            await using var cmd = _dbAccessFactory.CreateCommand();
            cmd.CommandText = _compiledSql.CommandText;
            cmd.Parameters.AddRange(_compiledSql.Parameters);
            return await cmd.ExecuteNonQueryAsync(_cancellationToken);
        }
    }
}

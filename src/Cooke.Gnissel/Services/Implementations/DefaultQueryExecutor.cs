#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Statements;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultQueryExecutor : IQueryExecutor
{
    public async IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> mapper,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var cmd = commandFactory.CreateCommand();
        dbAdapter.PopulateCommand(cmd, sql);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        cancellationToken.Register(reader.Close);
        await foreach (var value in mapper(reader, cancellationToken))
        {
            yield return value;
        }
    }

    public IExecuteStatement Execute(
        Sql sql,
        ICommandFactory commandFactory,
        IDbAdapter dbAdapter,
        CancellationToken cancellationToken
    )
    {
        return new ExecuteStatement(commandFactory, dbAdapter, sql);
    }

    private class ExecuteStatement : IExecuteStatement
    {
        private readonly ICommandFactory _commandFactory;
        private readonly IDbAdapter _dbAdapter;
        private readonly Sql _sql;

        public ExecuteStatement(ICommandFactory commandFactory, IDbAdapter dbAdapter, Sql sql)
        {
            _commandFactory = commandFactory;
            _dbAdapter = dbAdapter;
            _sql = sql;
        }

        public async ValueTask<int> ExecuteAsync(
            ICommandFactory? commandFactory = null,
            CancellationToken cancellationToken = default
        )
        {
            await using var cmd = (commandFactory ?? _commandFactory).CreateCommand();
            _dbAdapter.PopulateCommand(cmd, _sql);
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public ValueTaskAwaiter<int> GetAwaiter()
        {
            return ExecuteAsync().GetAwaiter();
        }
    }
}

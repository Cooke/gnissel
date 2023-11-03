#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Statements;
using Cooke.Gnissel.Utils;

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
        var (commandText, parameters) = dbAdapter.BuildSql(sql);
        cmd.CommandText = commandText;
        cmd.Parameters.AddRange(parameters);
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
        return new ExecuteStatement(commandFactory, dbAdapter, sql, cancellationToken);
    }

    private class ExecuteStatement : IExecuteStatement
    {
        private readonly ICommandFactory _commandFactory;
        private readonly IDbAdapter _dbAdapter;
        private readonly Sql _sql;
        private readonly CancellationToken _cancellationToken;

        public ExecuteStatement(
            ICommandFactory commandFactory,
            IDbAdapter dbAdapter,
            Sql sql,
            CancellationToken cancellationToken
        )
        {
            _commandFactory = commandFactory;
            _dbAdapter = dbAdapter;
            _sql = sql;
            _cancellationToken = cancellationToken;
        }

        public Sql Sql => _sql;

        public async ValueTask<int> ExecuteAsync(
            ICommandFactory? commandFactory = null,
            CancellationToken cancellationToken = default
        )
        {
            await using var cmd = (commandFactory ?? _commandFactory).CreateCommand();
            var (commandText, parameters) = _dbAdapter.BuildSql(_sql);
            cmd.CommandText = commandText;
            cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteNonQueryAsync(cancellationToken.Combine(_cancellationToken));
        }

        public ValueTaskAwaiter<int> GetAwaiter()
        {
            return ExecuteAsync().GetAwaiter();
        }
    }
}

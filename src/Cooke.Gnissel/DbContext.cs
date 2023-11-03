#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Statements;

#endregion

namespace Cooke.Gnissel;

public class DbContext
{
    private readonly IRowReader _rowReader;
    private readonly IDbAdapter _dbAdapter;
    private readonly ICommandFactory _commandFactory;
    private readonly IQueryExecutor _queryExecutor;

    public DbContext(DbOptions dbOptions)
    {
        _rowReader = dbOptions.RowReader;
        _dbAdapter = dbOptions.DbAdapter;
        _commandFactory = dbOptions.CommandFactory;
        _queryExecutor = dbOptions.QueryExecutor;
    }

    public IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        CancellationToken cancellationToken = default
    ) => Query(sql, _rowReader.Read<TOut>, cancellationToken);

    public IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        Func<DbDataReader, CancellationToken, IAsyncEnumerable<TOut>> mapper,
        CancellationToken cancellationToken = default
    ) => _queryExecutor.Query(sql, mapper, _commandFactory, _dbAdapter, cancellationToken);

    public IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        Func<DbDataReader, TOut> mapper,
        CancellationToken cancellationToken = default
    )
    {
        return _queryExecutor.Query(sql, Mapper, _commandFactory, _dbAdapter, cancellationToken);

        async IAsyncEnumerable<TOut> Mapper(
            DbDataReader reader,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                yield return mapper(reader);
            }
        }
    }

    public async Task Batch(List<IExecuteStatement> statements)
    {
        await using var connection = _dbAdapter.CreateConnection();
        await using var batch = connection.CreateBatch();
        foreach (var statement in statements)
        {
            var (commandText, parameters) = _dbAdapter.BuildSql(statement.Sql);
            var batchCommand = _dbAdapter.CreateBatchCommand();
            batchCommand.CommandText = commandText;
            batchCommand.Parameters.AddRange(parameters);
            batch.BatchCommands.Add(batchCommand);
        }
        await connection.OpenAsync();
        await batch.ExecuteNonQueryAsync();
    }

    public IExecuteStatement Execute(Sql sql, CancellationToken cancellationToken = default) =>
        _queryExecutor.Execute(sql, _commandFactory, _dbAdapter, cancellationToken);

    public async Task Transaction(IEnumerable<IExecuteStatement> statements)
    {
        await using var connection = _dbAdapter.CreateConnection();
        await connection.OpenAsync();
        var transactionCommandFactory = new ConnectionCommandFactory(connection, _dbAdapter);
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var statement in statements)
        {
            await statement.ExecuteAsync(transactionCommandFactory);
        }
        await transaction.CommitAsync();
    }
}

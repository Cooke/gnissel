#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Statements;

#endregion

namespace Cooke.Gnissel;

public class DbContext
{
    private readonly IRowReader _rowReader;
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbAccessFactory _dbAccessFactory;
    private readonly IQueryExecutor _queryExecutor;

    public DbContext(DbOptions dbOptions)
    {
        _rowReader = dbOptions.RowReader;
        _dbAdapter = dbOptions.DbAdapter;
        _dbAccessFactory = dbOptions.DbAccessFactory;
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
    ) =>
        _queryExecutor.Query(
            _dbAdapter.CompileSql(sql),
            mapper,
            _dbAccessFactory,
            cancellationToken
        );

    public IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        Func<DbDataReader, TOut> mapper,
        CancellationToken cancellationToken = default
    )
    {
        return _queryExecutor.Query(
            _dbAdapter.CompileSql(sql),
            Mapper,
            _dbAccessFactory,
            cancellationToken
        );

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

    public ExecuteStatement Execute(Sql sql, CancellationToken cancellationToken = default) =>
        new ExecuteStatement(_dbAccessFactory, _dbAdapter.CompileSql(sql), cancellationToken);

    public Task Batch(params ExecuteStatement[] statements) =>
        Batch((IEnumerable<ExecuteStatement>)statements);

    public async Task Batch(IEnumerable<ExecuteStatement> statements)
    {
        await using var batch = _dbAccessFactory.CreateBatch();
        foreach (var statement in statements)
        {
            var batchCommand = _dbAdapter.CreateBatchCommand();
            batchCommand.CommandText = statement.CompiledSql.CommandText;
            batchCommand.Parameters.AddRange(statement.CompiledSql.Parameters);
            batch.BatchCommands.Add(batchCommand);
        }
        await batch.ExecuteNonQueryAsync();
    }

    public async Task Transaction(IEnumerable<ExecuteStatement> statements)
    {
        await using var connection = _dbAccessFactory.CreateConnection();
        await using var batch = connection.CreateBatch();
        foreach (var statement in statements)
        {
            var batchCommand = _dbAdapter.CreateBatchCommand();
            batchCommand.CommandText = statement.CompiledSql.CommandText;
            batchCommand.Parameters.AddRange(statement.CompiledSql.Parameters);
            batch.BatchCommands.Add(batchCommand);
        }
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        batch.Transaction = transaction;
        await batch.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
}

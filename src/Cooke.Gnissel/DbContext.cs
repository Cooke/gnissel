#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel;

public class DbContext
{
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbConnector _dbConnector;
    private readonly IObjectReaderProvider _objectReaderProvider;

    public DbContext(DbOptions dbOptions)
    {
        _dbAdapter = dbOptions.DbAdapter;
        _dbConnector = dbOptions.DbConnector;
        _objectReaderProvider = dbOptions.ObjectReaderProvider;
    }

    public IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql
    ) =>
        _objectReaderProvider.Get<TOut>().Let(objectReader =>
            new Query<TOut>(
                _dbAdapter.RenderSql(sql),
                (reader, ct) => reader.ReadRows(objectReader, ct),
                _dbConnector
            ));

    public IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        Func<DbDataReader, TOut> mapper
    ) =>
        new Query<TOut>(
            _dbAdapter.RenderSql(sql),
            (reader, ct) => reader.ReadRows(mapper, ct),
            _dbConnector
        );

    public ExecuteQuery Execute(Sql sql, CancellationToken cancellationToken = default) =>
        new ExecuteQuery(_dbConnector, _dbAdapter.RenderSql(sql), cancellationToken);

    public Task Batch(params ExecuteQuery[] statements) =>
        Batch((IEnumerable<ExecuteQuery>)statements);

    public async Task Batch(IEnumerable<ExecuteQuery> statements)
    {
        await using var batch = _dbConnector.CreateBatch();
        foreach (var statement in statements) {
            var batchCommand = _dbAdapter.CreateBatchCommand();
            batchCommand.CommandText = statement.RenderedSql.CommandText;
            batchCommand.Parameters.AddRange(statement.RenderedSql.Parameters);
            batch.BatchCommands.Add(batchCommand);
        }
        await batch.ExecuteNonQueryAsync();
    }

    public Task Transaction(params ExecuteQuery[] statements) =>
        Transaction((IEnumerable<ExecuteQuery>)statements);

    public async Task Transaction(IEnumerable<ExecuteQuery> statements)
    {
        await using var connection = _dbConnector.CreateConnection();
        await using var batch = connection.CreateBatch();
        foreach (var statement in statements) {
            var batchCommand = _dbAdapter.CreateBatchCommand();
            batchCommand.CommandText = statement.RenderedSql.CommandText;
            batchCommand.Parameters.AddRange(statement.RenderedSql.Parameters);
            batch.BatchCommands.Add(batchCommand);
        }
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        batch.Transaction = transaction;
        await batch.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
}
#region

using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.History;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel;

public class DbContext(DbOptions dbOptions)
{
    private readonly IDbAdapter _dbAdapter = dbOptions.DbAdapter;
    private readonly IDbConnector _dbConnector = dbOptions.DbConnector;
    private readonly IObjectReaderProvider _objectReaderProvider = dbOptions.ObjectReaderProvider;

    [Pure]
    public Query<TOut> Query<TOut>(Sql sql) =>
        _objectReaderProvider
            .Get<TOut>(dbOptions)
            .Let(objectReader => new Query<TOut>(
                dbOptions.RenderSql(sql),
                (reader, ct) => reader.ReadRows(objectReader, ct),
                _dbConnector
            ));

    [Pure]
    public Query<TOut> Query<TOut>(Sql sql, Func<DbDataReader, TOut> mapper) =>
        new(dbOptions.RenderSql(sql), (reader, ct) => reader.ReadRows(mapper, ct), _dbConnector);

    [Pure]
    public SingleQuery<TOut> QuerySingle<TOut>(Sql sql) =>
        _objectReaderProvider
            .Get<TOut>(dbOptions)
            .Let(objectReader => new SingleQuery<TOut>(
                dbOptions.RenderSql(sql),
                (reader, ct) => reader.ReadRows(objectReader, ct),
                _dbConnector
            ));

    [Pure]
    public SingleQuery<TOut> QuerySingle<TOut>(Sql sql, Func<DbDataReader, TOut> mapper) =>
        new(dbOptions.RenderSql(sql), (reader, ct) => reader.ReadRows(mapper, ct), _dbConnector);

    [Pure]
    public SingleOrDefaultQuery<TOut> QuerySingleOrDefault<TOut>(Sql sql) =>
        _objectReaderProvider
            .Get<TOut>(dbOptions)
            .Let(objectReader => new SingleOrDefaultQuery<TOut>(
                dbOptions.RenderSql(sql),
                (reader, ct) => reader.ReadRows(objectReader, ct),
                _dbConnector
            ));

    [Pure]
    public SingleOrDefaultQuery<TOut> QuerySingleOrDefault<TOut>(
        Sql sql,
        Func<DbDataReader, TOut> mapper
    ) => new(dbOptions.RenderSql(sql), (reader, ct) => reader.ReadRows(mapper, ct), _dbConnector);

    [Pure]
    public NonQuery NonQuery(Sql sql) => new(_dbConnector, dbOptions.RenderSql(sql));

    public ValueTask Batch(params INonQuery[] statements) =>
        Batch((IEnumerable<INonQuery>)statements);

    public async ValueTask Batch(IEnumerable<INonQuery> statements)
    {
        await using var batch = _dbConnector.CreateBatch();
        foreach (var statement in statements)
        {
            var batchCommand = _dbAdapter.CreateBatchCommand();
            batchCommand.CommandText = statement.RenderedSql.CommandText;
            batchCommand.Parameters.AddRange(statement.RenderedSql.Parameters);
            batch.BatchCommands.Add(batchCommand);
        }
        await batch.ExecuteNonQueryAsync();
    }

    public ValueTask Transaction(params INonQuery[] statements) =>
        Transaction((IEnumerable<INonQuery>)statements);

    public async ValueTask Transaction(IEnumerable<INonQuery> statements)
    {
        await using var connection = _dbConnector.CreateConnection();
        await using var batch = connection.CreateBatch();
        foreach (var statement in statements)
        {
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

    public ValueTask Migrate(
        IReadOnlyList<Migration> migrations,
        CancellationToken cancellationToken = default
    ) => _dbAdapter.Migrator.Migrate(migrations, cancellationToken);
}

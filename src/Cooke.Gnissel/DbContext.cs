#region

using System.Data.Common;
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

    internal IDbAdapter Adapter => _dbAdapter;

    public IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        CancellationToken cancellationToken = default
    ) => Query(sql, _rowReader.Read<TOut>, cancellationToken);

    public IAsyncEnumerable<TOut> Query<TOut>(
        Sql sql,
        Func<DbDataReader, TOut> mapper,
        CancellationToken cancellationToken = default
    ) =>
        _queryExecutor.Execute(
            sql,
            mapper,
            _commandFactory,
            _dbAdapter,
            cancellationToken
        );

    public async Task Transaction(IEnumerable<IInsertStatement> statements)
    {
        await using var connection = _dbAdapter.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var statement in statements)
        {
            await statement.ExecuteAsync(connection);
        }
        await transaction.CommitAsync();
    }
}

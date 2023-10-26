using System.Data.Common;

namespace Cooke.Gnissel.Transactions;

public interface ITransactionContext<out TContext>
{
    TContext WithCommandFactory(ICommandFactory commandFactory);
}

public static class TransactionContextExtensions
{
    public static async Task Transaction<TContext>(
        this TContext context,
        Func<TContext, Task> action,
        CancellationToken cancellationToken = default
    )
        where TContext : DbContext, ITransactionContext<TContext>
    {
        var dbAdapter = context.Adapter;
        await using var connection = dbAdapter.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await action(
            context.WithCommandFactory(new TransactionCommandFactory(connection, dbAdapter))
        );
        await transaction.CommitAsync(cancellationToken);
    }

    private class TransactionCommandFactory : ICommandFactory
    {
        private readonly DbConnection _connection;
        private readonly IDbAdapter _adapter;

        public TransactionCommandFactory(DbConnection connection, IDbAdapter adapter)
        {
            _connection = connection;
            _adapter = adapter;
        }

        public DbCommand CreateCommand()
        {
            var command = _adapter.CreateCommand();
            command.Connection = _connection;
            return command;
        }
    }
}

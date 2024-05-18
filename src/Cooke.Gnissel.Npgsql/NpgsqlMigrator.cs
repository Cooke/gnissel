using System.Diagnostics;
using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.History;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Utils;
using Microsoft.Extensions.Logging;

namespace Cooke.Gnissel.Npgsql;

public class NpgsqlMigrator(ILogger logger, IDbAdapter dbAdapter) : IMigrator
{
    public async ValueTask Migrate(
        IReadOnlyCollection<IMigration> migrations,
        CancellationToken cancellationToken
    )
    {
        await CreateMigrationHistoryTableIfNotExist(dbAdapter, cancellationToken);

        await using var connection = dbAdapter.CreateConnector().CreateConnection();
        await connection.OpenAsync(cancellationToken);

        while (true)
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var dbContext = new DbContext(
                new DbOptions(dbAdapter, new FixedConnectionDbConnector(connection, dbAdapter))
            );
            await dbContext
                .NonQuery($"LOCK TABLE __migration_history")
                .ExecuteAsync(cancellationToken);
            var appliedMigrationNames = await dbContext
                .Query<string>($"SELECT id FROM __migration_history")
                .ToHashSetAsync(cancellationToken);

            var migration = migrations.FirstOrDefault(x => !appliedMigrationNames.Contains(x.Id));
            if (migration == null)
            {
                break;
            }

            var sw = Stopwatch.StartNew();
            logger.LogInformation("Applying migration: {Id}", migration.Id);
            await migration.Migrate(dbContext, cancellationToken);
            await dbContext
                .NonQuery(
                    $"INSERT INTO __migration_history (id, applied_at) VALUES ({migration.Id}, now())"
                )
                .ExecuteAsync(cancellationToken);
            logger.LogInformation("Applied migration: {Id} in {Elapsed}", migration.Id, sw.Elapsed);

            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static async Task CreateMigrationHistoryTableIfNotExist(
        IDbAdapter dbAdapter,
        CancellationToken cancellationToken
    )
    {
        var initialOptions = new DbOptions(dbAdapter);
        var initialContext = new DbContext(initialOptions);

        await initialContext
            .NonQuery(
                $@"
                CREATE TABLE IF NOT EXISTS __migration_history
                (
                    id text primary key,
                    applied_at timestamp with time zone
                );
            "
            )
            .ExecuteAsync(cancellationToken);
    }
}

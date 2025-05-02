using System.Diagnostics;
using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Services.Implementations;
using Microsoft.Extensions.Logging;

namespace Cooke.Gnissel.Npgsql;

public partial class NpgsqlDbAdapter
{
    public async ValueTask Migrate(
        IReadOnlyCollection<Migration> migrations,
        CancellationToken cancellationToken
    )
    {
        await CreateMigrationHistoryTableIfNotExist(cancellationToken);

        await using var connection = this.CreateConnector().CreateConnection();
        await connection.OpenAsync(cancellationToken);

        while (true)
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var dbContext = new DbContext(
                new DbOptions(
                    this,
                    new MapperProvider(),
                    new FixedConnectionDbConnector(connection, this)
                )
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
            _logger.LogInformation("Applying migration: {Id}", migration.Id);
            await migration.Migrate(dbContext, cancellationToken);
            await dbContext
                .NonQuery(
                    $"INSERT INTO __migration_history (id, applied_at) VALUES ({migration.Id}, now())"
                )
                .ExecuteAsync(cancellationToken);
            _logger.LogInformation(
                "Applied migration: {Id} in {Elapsed}",
                migration.Id,
                sw.Elapsed
            );

            await transaction.CommitAsync(cancellationToken);
        }
    }

    private async Task CreateMigrationHistoryTableIfNotExist(CancellationToken cancellationToken)
    {
        var initialOptions = new DbOptions(this, new MapperProvider());
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

    private class FieldValueObjectReaderProvider : IObjectReaderProvider
    {
        public ObjectReader<TOut> Get<TOut>() =>
            new(
                (reader, ordinalReader) => reader.GetFieldValue<TOut>(ordinalReader.Read()),
                () => [new NextOrdinalReadDescriptor()]
            );
    }

    private class FieldValueObjectWriterProvider : IObjectWriterProvider
    {
        public ObjectWriter<TOut> Get<TOut>() =>
            new(
                (value, writer) => writer.Write(value),
                () => [new UnspecifiedColumnWriteDescriptor()]
            );

        public IObjectWriter Get(Type type) => throw new NotSupportedException();
    }

    private class MapperProvider : IMapperProvider
    {
        public IObjectReaderProvider ReaderProvider { get; } = new FieldValueObjectReaderProvider();

        public IObjectWriterProvider WriterProvider { get; } = new FieldValueObjectWriterProvider();

        public NamingConvention NamingConvention { get; } = NamingConvention.AsIs;
    }
}

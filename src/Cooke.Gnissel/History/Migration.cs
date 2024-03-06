namespace Cooke.Gnissel.History;

public interface IMigration
{
    string Id { get; }

    ValueTask Migrate(DbContext db, CancellationToken cancellationToken);
}

public record Migration(string Id, Func<DbContext, CancellationToken, ValueTask> Migrate)
    : IMigration
{
    ValueTask IMigration.Migrate(DbContext db, CancellationToken cancellationToken) =>
        Migrate(db, cancellationToken);
}

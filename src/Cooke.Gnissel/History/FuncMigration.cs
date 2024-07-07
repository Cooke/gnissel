namespace Cooke.Gnissel.History;

public abstract class Migration
{
    public abstract string Id { get; }

    public abstract ValueTask Migrate(DbContext db, CancellationToken cancellationToken);
}

public sealed class FuncMigration(string id, Func<DbContext, CancellationToken, ValueTask> migrate)
    : Migration
{
    public override string Id => id;

    public override ValueTask Migrate(DbContext db, CancellationToken cancellationToken) =>
        migrate(db, cancellationToken);
}

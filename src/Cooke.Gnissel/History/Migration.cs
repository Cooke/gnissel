namespace Cooke.Gnissel.History;

public record Migration(string Id, Func<DbContext, CancellationToken, ValueTask> Migrate) { }

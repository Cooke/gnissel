namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class BaseDbContext(DbOptions dbOptions)
        : DbContext(dbOptions, new GeneratedObjectReaderProvider(dbOptions.DbAdapter)) { }
}

using Cooke.Gnissel.History;

namespace Cooke.Gnissel.Services;

public interface IMigrator
{
    ValueTask MigrateAsync(
        IReadOnlyCollection<Migration> migrations,
        CancellationToken cancellationToken
    );
}

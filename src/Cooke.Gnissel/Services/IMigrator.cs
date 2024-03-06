using Cooke.Gnissel.History;

namespace Cooke.Gnissel.Services;

public interface IMigrator
{
    ValueTask Migrate(
        IReadOnlyCollection<IMigration> migrations,
        CancellationToken cancellationToken
    );
}

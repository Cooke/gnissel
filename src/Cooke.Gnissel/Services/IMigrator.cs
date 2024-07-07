namespace Cooke.Gnissel.Services;

public interface IMigrator
{
    ValueTask Migrate(
        IReadOnlyCollection<Migration> migrations,
        CancellationToken cancellationToken
    );
}

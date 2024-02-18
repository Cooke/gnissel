namespace Cooke.Gnissel.Queries;

public interface INonQuery : IQuery
{
    ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default);
}

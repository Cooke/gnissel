namespace Cooke.Gnissel.Queries;

public interface IQuery<out T>
{
    // Future intention is for this be able to use in batch queries
    RenderedSql RenderedSql { get; }

    IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default);
}

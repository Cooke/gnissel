namespace Cooke.Gnissel.Queries;

public interface INonQuery
{
    ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default);
    
    RenderedSql RenderedSql { get; }
}

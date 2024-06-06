namespace Cooke.Gnissel.Queries;

public interface IQuery<out T> : IAsyncEnumerable<T>
{
    RenderedSql RenderedSql { get; }
}

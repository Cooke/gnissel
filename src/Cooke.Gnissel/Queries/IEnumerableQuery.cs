namespace Cooke.Gnissel.Queries;

public interface IEnumerableQuery<out T> : IQuery, IAsyncEnumerable<T> { }
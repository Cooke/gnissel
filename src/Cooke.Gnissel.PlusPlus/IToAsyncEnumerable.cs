using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.PlusPlus;

public interface IToAsyncEnumerable<out T>
{
    public IAsyncEnumerable<T> ToAsyncEnumerable();
}

public static class ToAsyncEnumerableExtensions
{
    public static ValueTask<T[]> ToArrayAsync<T>(
        this IToAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default
    ) => source.ToAsyncEnumerable().ToArrayAsync(cancellationToken);
}

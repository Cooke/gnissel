namespace PlusPlusLab.Querying;

public interface IToAsyncEnumerable<out T>
{
    public IAsyncEnumerable<T> ToAsyncEnumerable();
}
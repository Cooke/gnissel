namespace Cooke.Gnissel;

public interface IObjectWriterProvider
{
    ObjectWriter<T> Get<T>();
}

namespace Cooke.Gnissel.Services;

public interface IObjectReaderProvider
{
    ObjectReader<TOut> Get<TOut>();

    IObjectReader Get(Type type);
}

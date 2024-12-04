namespace Cooke.Gnissel.Services;

public interface IObjectReaderProvider
{
    ObjectReader<TOut> Get<TOut>(DbOptions dbOptions);
}

namespace Cooke.Gnissel.Services;

public interface IObjectReaderFactory
{
    ObjectReader<TOut> Create<TOut>(DbOptions dbOptions);
}

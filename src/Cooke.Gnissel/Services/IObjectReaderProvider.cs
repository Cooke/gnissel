#region

#endregion

namespace Cooke.Gnissel.Services;

public interface IObjectReaderProvider
{
    ObjectReader<TOut> Get<TOut>(DbRuntimeContext dbRuntimeContext);
}

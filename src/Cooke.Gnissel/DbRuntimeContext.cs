using System.Collections.Concurrent;

namespace Cooke.Gnissel;

public class DbRuntimeContext(DbOptions dbOptions)
{
    private ConcurrentBag<Db>
    public bool TryGetConverter(Type type, out IDbConverter o)
    {
        throw new NotImplementedException();
    }
}

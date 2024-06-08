using System.Runtime.CompilerServices;

namespace Cooke.Gnissel.Queries;

public interface INonQuery
{
    RenderedSql RenderedSql { get; }

    ValueTaskAwaiter<int> GetAwaiter();
}

using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Querying;

public interface IToQuery<T>
{
    public Query<T> ToQuery();
}

using Cooke.Gnissel.Queries;

namespace Cooke.Gnissel.Typed.Queries;

public interface IToQuery<T>
{
    public Query<T> ToQuery();
}

using Cooke.Gnissel.Queries;

namespace PlusPlusLab.Querying;

public interface IToQuery<T>
{
    public Query<T> ToQuery();
}

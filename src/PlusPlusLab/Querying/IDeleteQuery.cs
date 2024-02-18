using System.Linq.Expressions;

namespace PlusPlusLab.Querying;

public interface IDeleteQuery
{
    ITable Table { get; }

    Expression? Condition { get; }
}

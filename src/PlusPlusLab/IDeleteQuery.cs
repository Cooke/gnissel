using System.Linq.Expressions;

namespace PlusPlusLab;

public interface IDeleteQuery
{
    ITable Table { get; }

    Expression? Condition { get; }
}

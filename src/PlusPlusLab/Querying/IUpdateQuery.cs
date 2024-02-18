using System.Linq.Expressions;

namespace PlusPlusLab.Querying;

public interface IUpdateQuery
{
    ITable Table { get; }

    Expression? Condition { get; }

    IReadOnlyCollection<Setter> Setters { get; }
}

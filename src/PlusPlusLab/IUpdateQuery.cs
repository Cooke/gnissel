using System.Linq.Expressions;

namespace PlusPlusLab;

public interface IUpdateQuery
{
    ITable Table { get; }

    Expression? Condition { get; }

    IReadOnlyCollection<Setter> Setters { get; }
}

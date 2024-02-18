using System.Linq.Expressions;

namespace PlusPlusLab.Querying;

public record Setter(IColumn Column, Expression Value);

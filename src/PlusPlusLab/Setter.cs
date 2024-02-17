using System.Linq.Expressions;

namespace PlusPlusLab;

public record Setter(IColumn Column, Expression Value);

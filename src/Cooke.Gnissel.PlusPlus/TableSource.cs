using System.Linq.Expressions;

namespace Cooke.Gnissel.PlusPlus;

public record TableSource(ITable Table, Expression Expression);

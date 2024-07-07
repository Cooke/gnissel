using Cooke.Gnissel.Typed.Queries;

namespace Cooke.Gnissel.Typed.Services;

public interface ITypedSqlGenerator
{
    Sql Generate(IInsertQuery query);

    Sql Generate(IDeleteQuery query);

    Sql Generate(IUpdateQuery query);

    Sql Generate(ExpressionQuery query);
}

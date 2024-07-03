using Cooke.Gnissel.Typed.Queries;

namespace Cooke.Gnissel.Typed.Services;

public interface ITypedSqlGenerator
{
    Sql Generate(IInsertQuery query, DbOptions dbOptions);

    Sql Generate(IDeleteQuery query, DbOptions dbOptions);

    Sql Generate(IUpdateQuery query, DbOptions dbOptions);

    Sql Generate(ExpressionQuery query, DbOptions dbOptions);
}

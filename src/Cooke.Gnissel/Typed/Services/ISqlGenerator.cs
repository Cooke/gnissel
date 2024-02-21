using Cooke.Gnissel.Typed.Queries;

namespace Cooke.Gnissel.Typed.Services;

public interface ISqlGenerator
{
    Sql Generate(IInsertQuery query);

    Sql Generate(IDeleteQuery query);

    Sql Generate(IUpdateQuery query);
    
    Sql Generate(ExpressionQuery query);
    
}

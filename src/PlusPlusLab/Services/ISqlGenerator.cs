using Cooke.Gnissel;
using PlusPlusLab.Querying;

namespace PlusPlusLab.Services;

public interface ISqlGenerator
{
    Sql Generate(IInsertQuery query);

    Sql Generate(IDeleteQuery query);

    Sql Generate(IUpdateQuery query);
    
    Sql Generate(ExpressionQuery query);
    
}

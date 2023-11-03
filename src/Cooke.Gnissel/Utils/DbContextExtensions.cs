using Cooke.Gnissel.Services.Implementations;
using Cooke.Gnissel.Statements;

namespace Cooke.Gnissel.Utils;

public static class DbContextExtensions
{
    public static Task Transaction(this DbContext dbContext, params ExecuteStatement[] statements)
    {
        return dbContext.Transaction(statements);
    }
}
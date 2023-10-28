using Cooke.Gnissel.Statements;

namespace Cooke.Gnissel.Utils;

public static class DbContextExtensions
{
    public static Task Transaction(this DbContext dbContext, params IInsertStatement[] statements)
    {
        return dbContext.Transaction(statements);
    }
}
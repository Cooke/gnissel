using System.Diagnostics.Contracts;

namespace Cooke.Gnissel.SourceGeneration.Test;

public class Program
{
    public void Main()
    {
        var dbContext = new AppDbContext();
        var query = dbContext.Query<User>("SELECT * FROM Users");
    }

    public class User;

    public class AppDbContext : DbContext { }

    public abstract class DbContext
    {
        [Pure]
        public Query<TOut> Query<TOut>(string sql) => new Query<TOut>();
    }

    public class Query<T> { }
}

using System.Diagnostics.Contracts;

namespace Cooke.Gnissel.SourceGeneration.Test;

public class Program
{
    public void Main()
    {
        var dbContext = new AppDbContext();
        var query = dbContext.Query<User>("SELECT * FROM Users");

        dbContext.Query<(User, Device)>("SELECT * FROM Users, Devices");

        // dbContext.Query<int>("SELECT COUNT(*) FROM Users");

        dbContext.Query<int?>("SELECT COUNT(*) FROM Users");
    }

    public class User;

    public class Device;
}

public class Query<T> { }

public abstract class DbContext
{
    [Pure]
    public Query<TOut> Query<TOut>(string sql) => new Query<TOut>();
}

public partial class AppDbContext : DbContext { }

namespace Cooke.Gnissel.SourceGeneration.AutoTests;

public class ReaderTests
{
    [Fact]
    public Task Int()
    {
        var source = """
            using Cooke.Gnissel;
            using Cooke.Gnissel.Services;

            var dbContext = new AppDbContext((IDbAdapter)null!);
            var query = dbContext.Query<int>($"SELECT count(*) FROM Users");

            [DbContext]
            public partial class AppDbContext;
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task NullableInt()
    {
        var source = """
            using System.Diagnostics.Contracts;

            namespace Cooke.Gnissel.SourceGeneration.Test;

            public class Program
            {
                public void Main()
                {
                    var dbContext = new AppDbContext();
                    var query = dbContext.Query<int?>("SELECT count(*) FROM Users");
                }
            }

            public class Query<T> { }

            public abstract class DbContext
            {
                [Pure]
                public Query<TOut> Query<TOut>(string sql) => new Query<TOut>();
            }

            public partial class AppDbContext : DbContext { }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task TupleOfNullableInts()
    {
        var source = """
            using System.Diagnostics.Contracts;

            namespace Cooke.Gnissel.SourceGeneration.Test;

            public class Program
            {
                public void Main()
                {
                    var dbContext = new AppDbContext();
                    var query = dbContext.Query<(int?, int)>("SELECT count(*) FROM Users");
                }
            }

            public class Query<T> { }

            public abstract class DbContext
            {
                [Pure]
                public Query<TOut> Query<TOut>(string sql) => new Query<TOut>();
            }

            public partial class AppDbContext : DbContext { }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task Complex()
    {
        var source = """
            using System.Diagnostics.Contracts;

            namespace Cooke.Gnissel.SourceGeneration.Test;

            public class Program
            {
                public void Main()
                {
                    var dbContext = new AppDbContext();
                    var query = dbContext.Query<User>("SELECT count(*) FROM Users");
                }

                public class User(string name, int age, int? size);
            }

            public class Query<T> { }

            public abstract class DbContext
            {
                [Pure]
                public Query<TOut> Query<TOut>(string sql) => new Query<TOut>();
            }

            public partial class AppDbContext : DbContext { }
            """;

        return TestHelper.Verify(source);
    }
}

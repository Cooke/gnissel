using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed.Test.Fixtures;
using Xunit.Abstractions;

namespace Cooke.Gnissel.Typed.Test;

[Collection("Database collection")]
public partial class MappingTests : IDisposable
{
    private NpgsqlDbAdapter _npgsqlDbAdapter;

    public MappingTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper)
    {
        databaseFixture.SetOutputHelper(testOutputHelper);
        _npgsqlDbAdapter = new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.Build());
        var db = new DbContext(_npgsqlDbAdapter, new DbMappers());
        db.NonQuery(
                $"""
                    create table accounts
                    (
                        identifier integer primary key,
                        "UserName" text,
                        age  integer,
                        "Street" text,
                        "the_city" text
                    );
                """
            )
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose()
    {
        var db = new DbContext(new(_npgsqlDbAdapter, new DbMappers()));
        db.NonQuery($"DROP TABLE accounts").GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Insert()
    {
        var usersTable = new Table<User>(
            new TableOptions(new DbOptions(_npgsqlDbAdapter, new DbMappers())) { Name = "accounts" }
        );
        await usersTable.Insert(
            new User(1, "Bob", 25, new UserAddress("bob's street", "Bob town"))
        );
        await usersTable.Insert(
            new User(2, "Sara", 25, new UserAddress("Sarah's street", "Sara town"))
        );

        var users = await usersTable.ToArrayAsync();

        Assert.Equal(new[] { (1, "Bob"), (2, "Sara") }, users.Select(x => (x.Id, x.Name)));
    }

    private record User(
        [DbName("identifier")] int Id,
        [property: DbName("UserName")] string Name,
        int Age,
        [property: DbName(null)] UserAddress Address
    )
    {
        public int UnmappedAge => Age;
    }

    private record UserAddress(
        [DbName("Street")] string Street,
        [property: DbName("the_city")] string City
    );

    [DbMappers(NamingConvention = NamingConvention.SnakeCase)]
    private partial class DbMappers;
}

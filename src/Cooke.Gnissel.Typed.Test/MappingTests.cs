using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed.Test.Fixtures;
using Cooke.Gnissel.Utils;
using Npgsql;
using Xunit.Abstractions;

namespace Cooke.Gnissel.Typed.Test;

[Collection("Database collection")]
public class MappingTests : IDisposable
{
    private NpgsqlDbAdapter _npgsqlDbAdapter;

    public MappingTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper)
    {
        databaseFixture.SetOutputHelper(testOutputHelper);
        _npgsqlDbAdapter = new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.Build());
        var db = new DbContext(new(_npgsqlDbAdapter));
        db.NonQuery(
                $"""
                    create table accounts
                    (
                        identifier   integer primary key,
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
        var db = new DbContext(new(_npgsqlDbAdapter));
        db.NonQuery($"DROP TABLE accounts").GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Insert()
    {
        var options = new TableOptionsBuilder<User>(new(_npgsqlDbAdapter))
            .Name("accounts")
            .Column(x => x.Id, x => x.Name("identifier"))
            .Column(x => x.Name, "UserName")
            .Column(x => x.Address.Street, x => x.Name("Street"))
            .Column(x => x.Address.City, x => x.Name("the_city"))
            .Ignore(x => x.UnmappedAge)
            .Build();
        var usersTable = new Table<User>(options);
        await usersTable.Insert(
            new User(1, "Bob", 25, new UserAddress("bob's street", "Bob town"))
        );
        await usersTable.Insert(
            new User(2, "Sara", 25, new UserAddress("Sarah's street", "Sara town"))
        );

        var users = await usersTable.ToArrayAsync();

        Assert.Equal(new[] { (1, "Bob"), (2, "Sara") }, users.Select(x => (x.Id, x.Name)));
    }

    private record User(int Id, string Name, int Age, UserAddress Address)
    {
        public int UnmappedAge => Age;
    }

    private record UserAddress(string Street, string City);
}

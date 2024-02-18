using Cooke.Gnissel;
using Cooke.Gnissel.Npgsql;
using PlusPlusLab.Test.Fixtures;
using Xunit.Abstractions;

namespace PlusPlusLab.Test;

[Collection("Database collection")]
public class JsonTests : IDisposable
{
    private readonly TestDbContext db;

    public JsonTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper)
    {
        databaseFixture.SetOutputHelper(testOutputHelper);

        db = new TestDbContext(
            new DbOptionsPlus(
                new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.EnableDynamicJson().Build())
            )
        );
        db.NonQuery(
                $"""
                    create table users
                    (
                        id   integer primary key,
                        data jsonb
                    );
                """
            )
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose()
    {
        db.NonQuery($"DROP TABLE users").GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Insert()
    {
        var data = new UserData("Bob", 25);
        var user = new User(1, data);
        await db.Users.Insert(user);

        var userFromDb = await db.Users.First();

        Assert.Equal(user, userFromDb);
    }

    private class TestDbContext(DbOptionsPlus options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }

    private record User(int Id, UserData Data);

    [DbType("jsonb")]
    private record UserData(string Username, int Level);
}

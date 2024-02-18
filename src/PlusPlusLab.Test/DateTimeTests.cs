using Cooke.Gnissel;
using Cooke.Gnissel.Npgsql;
using PlusPlusLab.Test.Fixtures;
using Xunit.Abstractions;

namespace PlusPlusLab.Test;

[Collection("Database collection")]
public class DateTimeTests : IDisposable
{
    private readonly TestDbContext db;

    public DateTimeTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper)
    {
        databaseFixture.SetOutputHelper(testOutputHelper);
        db = new TestDbContext(
            new DbOptionsPlus(new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.Build()))
        );
        db.NonQuery(
                $"""
                    create table users
                    (
                        id   integer primary key,
                        timestamp_tz  timestamp with time zone,
                        timestamp    timestamp without time zone
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
        var timestampTz = new DateTime(2000, 01, 01, 12, 0, 0, DateTimeKind.Utc);
        var timestamp = new DateTime(2000, 01, 01, 12, 0, 0, DateTimeKind.Utc);
        var user = new User(1, timestampTz, timestamp);
        await db.Users.Insert(user);

        var userFromDb = await db.Users.First();

        Assert.Equal(user, userFromDb);
    }

    [Fact]
    public async Task Update()
    {
        var timestampTz = new DateTime(2000, 01, 01, 12, 0, 0, DateTimeKind.Utc);
        var timestamp = new DateTime(3000, 01, 01, 12, 0, 0, DateTimeKind.Utc);
        var user = new User(1, timestampTz, timestamp);
        await db.Users.Insert(user);

        await db.Users.Update(x => true, s => s.Set(x => x.timestamp, x => x.timestampTz));
        var userFromDb = await db.Users.First();

        Assert.Equal(user with { timestamp = timestampTz }, userFromDb);
    }

    private class TestDbContext(DbOptionsPlus options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }

    private record User(int Id, DateTime timestampTz, DateTime timestamp);
}

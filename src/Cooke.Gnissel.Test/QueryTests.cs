#region

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed;
using Cooke.Gnissel.Utils;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class QueryTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder
    // .EnableDynamicJsonMappings()
    .Build();
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(new(new NpgsqlDbAdapter(_dataSource)));

        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
                        id   integer primary key generated always as identity,
                        name text,
                        age  integer
                    );

                    create table devices
                    (
                        id   text primary key,
                        name text,
                        user_id  integer
                    );
                """
            )
            .ExecuteNonQueryAsync();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dataSource.CreateCommand("DROP TABLE users").ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE devices RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task QueryParameters()
    {
        const string name = "Bob";
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<User>($"SELECT * FROM users WHERE name={name}")
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task QueryParametersWithType()
    {
        const string name = "Bob";
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<User>(
                $"SELECT * FROM users WHERE to_jsonb(name) = {JsonSerializer.Serialize(name):jsonb}"
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task QueryInject()
    {
        const string name = "Bob";
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<User>(
                $"SELECT * FROM {Sql.Inject("users")} WHERE name={name}"
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task QueryJoin()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Devices.Insert(new Device(new DeviceId("my-device"), "IPhone", 1));
        var results = await _db.Query<(User, Device)>(
                $"SELECT * FROM users JOIN devices ON users.id=devices.user_id"
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(
            new[] { (new User(1, "Bob", 25), new Device(new DeviceId("my-device"), "IPhone", 1)) },
            results
        );
    }

    [Test]
    [Timeout(1000)]
    public async Task CancelOperations()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Bob", 25));
        for (var i = 0; i < 100; i++)
        {
            using var cts = new CancellationTokenSource();
            var enumerator = _db.Query<(int, string, int)>($"SELECT * FROM users")
                .GetAsyncEnumerator(cts.Token);
            await enumerator.MoveNextAsync();
            cts.Cancel();
        }
    }

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);

        public Table<Device> Devices { get; } = new(options);
    }

    private record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age
    );

    public record Device(DeviceId Id, string Name, int UserId);

    public record DeviceId([property: DbName("id")] [DbName("id")] string Value);
}

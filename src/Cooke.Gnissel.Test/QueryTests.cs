using System.ComponentModel.DataAnnotations.Schema;
using Cooke.Gnissel.Npgsql;
using Npgsql;

namespace Cooke.Gnissel.Test;

public class QueryTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSource;
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(_dataSource);

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
    public async Task QueryCustomNameMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query(
                $"SELECT * FROM users",
                x => new User(x.Get<int>("id"), x.Get<string>("name"), x.Get<int>("age"))
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task QueryCustomNameMappingCollidingColumns()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Devices.Insert(new Device("my-device", "Bob", 1));
        var results = await _db.Query(
                $"SELECT * FROM users JOIN devices ON users.id = devices.user_id",
                x => new User(x.Get<int>("id"), x.Get<string>("name"), x.Get<int>("age"))
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task QueryCustomOrdinalMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query(
                $"SELECT * FROM users",
                x => new User(x.Get<int>(0), x.Get<string>(1), x.Get<int>(2))
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task QueryClassMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task QueryTupleMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<(int, string, int)>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { (1, "Bob", 25) }, results);
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
            var enumerator = _db.Query<(int, string, int)>($"SELECT * FROM users", cts.Token)
                .GetAsyncEnumerator(cts.Token);
            await enumerator.MoveNextAsync(cts.Token);
            cts.Cancel();
        }
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(NpgsqlDataSource dataSource)
            : base(new NpgsqlDbAdapter(dataSource)) { }

        public Table<User> Users => Table<User>();

        public Table<Device> Devices => Table<Device>();
    }

    private record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age
    );

    public record Device(string Id, string Name, int UserId);
}
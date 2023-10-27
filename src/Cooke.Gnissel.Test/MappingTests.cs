#region

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Cooke.Gnissel.Npgsql;
using Npgsql;
using NpgsqlTypes;

#endregion

namespace Cooke.Gnissel.Test;

public class MappingTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSource;
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(new DbOptions(new NpgsqlDbAdapter(_dataSource)));

        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
                        id   integer primary key generated always as identity,
                        name text,
                        age  integer,
                        data jsonb
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
        _dataSource.CreateCommand("DROP TABLE devices").ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE devices RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task CustomNameMapping()
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
    public async Task CustomNameMappingCollidingColumns()
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
    public async Task CustomOrdinalMapping()
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
    public async Task ClassConstructorMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task ClassPropertyMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, results);
    }

    [Test]
    public async Task TupleMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<(int, string, int)>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { (1, "Bob", 25) }, results);
    }

    [Test]
    public async Task SingleValueMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<string>($"SELECT name FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { "Bob" }, results);
    }

    [Test]
    public async Task JsonMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25) { Data = new UserData("bob", 1) });
        var results = await _db.Query<string>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { "Bob" }, results);
    }

    [Test]
    [Ignore("Should warn when over fetching")]
    public async Task NotAllValuesMapped()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<string>($"SELECT name, id FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { "Bob" }, results);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbOptions options)
            : base(options)
        {
            Users = new Table<User>(options);
            Devices = new Table<Device>(options);
        }

        public Table<User> Users { get; }

        public Table<Device> Devices { get; }
    }

    private record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age
    )
    {
        [DataType("jsonb")]
        public UserData Data { get; init; }
    }

    private record UserData(string Username, int Level);

    public record Device(string Id, string Name, int UserId);
}

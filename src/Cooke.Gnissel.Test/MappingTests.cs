#region

using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Text.Json;
using Cooke.Gnissel.Npgsql;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class MappingTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSource;
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(
            new DbOptions(
                new NpgsqlDbAdapter(_dataSource),
                new ObjectMapper(new NpgsqlObjectMapperValueReader(new JsonSerializerOptions()))
            )
        );

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
        _dataSource.CreateCommand("DROP TABLE devices").ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE devices RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task CustomMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query(
                $"SELECT * FROM users",
                x => new User(x.GetInt32(0), x.GetString(x.GetOrdinal("name")), x.GetInt32("age"))
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
    );

    public record Device(string Id, string Name, int UserId);
}

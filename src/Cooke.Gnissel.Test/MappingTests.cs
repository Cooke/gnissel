#region

using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.PlusPlus;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class MappingTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
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
                        age  integer
                    );

                    create table devices
                    (
                        id   text primary key,
                        name text,
                        user_id  integer
                    );

                    create table dates
                    (
                        timestamp_with_timezone timestamp with time zone,
                        timestamp_without_timezone timestamp without time zone
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
        _dataSource.CreateCommand("DROP TABLE dates").ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE devices RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE dates RESTART IDENTITY CASCADE").ExecuteNonQuery();
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
    public async Task TimestampMapping()
    {
        DateTime withTimeZone = new DateTime(2023, 11, 7, 19, 4, 01, DateTimeKind.Utc);
        DateTime withoutTimeZone = new DateTime(2023, 11, 7, 19, 4, 01, DateTimeKind.Local);
        await _db.Execute(
            $"INSERT INTO dates (timestamp_with_timezone, timestamp_without_timezone) VALUES ({withTimeZone}, {withoutTimeZone})"
        );
        var results = await _db.Query<(DateTime WithTimezone, DateTime WithoutTimezone)>(
                $"SELECT timestamp_with_timezone, timestamp_without_timezone FROM dates"
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { (withTimeZone, withoutTimeZone) }, results);
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

#region

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Services.Implementations;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class MappingJsonTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSource;
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(
            new DbOptions(
                new NpgsqlDbAdapter(_dataSource),
                new DefaultObjectMapper(new NpgsqlObjectMapperValueReader(new JsonSerializerOptions()))
            )
        );

        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
                        id   integer primary key,
                        name text,
                        age  integer,
                        data jsonb
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
    public async Task JsonMapping()
    {
        var inserted = new User(0, "Bob", 25, new UserData("bob", 1));
        await _db.Users.Insert(inserted);
        var results = await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { inserted }, results);
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

    private record User(int Id, string Name, int Age, [property: DataType("jsonb")] UserData Data);

    private record UserData(string Username, int Level);

    public record Device(string Id, string Name, int UserId);
}

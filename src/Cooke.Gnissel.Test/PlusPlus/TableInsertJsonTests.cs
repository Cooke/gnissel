#region

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.PlusPlus;
using Dapper;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test.PlusPlus;

public class TableInsertJsonTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder
    // .EnableDynamicJsonMappings()
    .Build();
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
                """
            )
            .ExecuteNonQueryAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _db = new TestDbContext(new DbOptions(new NpgsqlDbAdapter(_dataSource)));

        await _dataSource.CreateCommand("drop table users;").ExecuteNonQueryAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task Insert()
    {
        await _db.Users.Insert(new User(0, "Bob", 25, new UserData("bob", 1)));

        var fetched = _dataSource.OpenConnection().QuerySingle("SELECT * FROM users");
        var fetchedTyped = new User(
            fetched.id,
            fetched.name,
            fetched.age,
            JsonSerializer.Deserialize<UserData>(fetched.data)
        );
        Assert.That(fetchedTyped, Is.EqualTo(new User(1, "Bob", 25, new UserData("bob", 1))));
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbOptions options)
            : base(options)
        {
            Users = new Table<User>(options);
        }

        public Table<User> Users { get; }
    }

    public record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age,
        [property: DbType("jsonb")] UserData Data
    );

    public record UserData(string Username, int Level);
}

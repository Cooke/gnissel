#region

using System.ComponentModel.DataAnnotations.Schema;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.PlusPlus;
using Dapper;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test.PlusPlus;

public class TableInsertTests
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
    public async Task InsertOne()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));

        var fetched = _dataSource.OpenConnection().QuerySingle<User>("SELECT * FROM users");
        Assert.That(fetched, Is.EqualTo(new User(1, "Bob", 25)));
    }

    [Test]
    public async Task InsertTwoSerial()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Alice", 30));

        var fetched = _dataSource.OpenConnection().Query<User>("SELECT * FROM users").ToArray();
        Assert.That(fetched.Length, Is.EqualTo(2));
        CollectionAssert.AreEqual(
            new[] { new User(1, "Bob", 25), new User(2, "Alice", 30) },
            fetched
        );
    }

    [Test]
    public async Task InsertMany()
    {
        await _db.Users.Insert(
            new User(0, "Bob", 25),
            new User(0, "Alice", 30),
            new User(0, "Slurf", 35)
        );

        var fetched = _dataSource.OpenConnection().Query<User>("SELECT * FROM users").ToArray();
        CollectionAssert.AreEqual(
            new[] { new User(1, "Bob", 25), new User(2, "Alice", 30), new User(3, "Slurf", 35) },
            fetched
        );
    }

    [Test]
    public async Task InsertBatch()
    {
        await _db.Batch(
            _db.Users.Insert(new User(0, "Bob", 25)),
            _db.Users.Insert(new User(0, "Alice", 30)),
            _db.Users.Insert(new User(0, "Slurf", 35))
        );

        var fetched = _dataSource.OpenConnection().Query<User>("SELECT * FROM users").ToArray();
        CollectionAssert.AreEqual(
            new[] { new User(1, "Bob", 25), new User(2, "Alice", 30), new User(3, "Slurf", 35) },
            fetched
        );
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
        int Age
    );
}

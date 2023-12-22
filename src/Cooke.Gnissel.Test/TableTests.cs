#region

using System.ComponentModel.DataAnnotations.Schema;
using Cooke.Gnissel.Npgsql;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class TableTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private TestDbContext _db = null!;

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
    public async Task Query()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 26));
        var users = await _db.Users.ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25), new User(2, "Sara", 26) }, users);
    }

    [Test]
    public async Task WhereStringConstant()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var users = await _db.Users.Where(x => x.Name == "Bob").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, users);
    }

    [Test]
    public async Task WhereIntConstant()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var users = await _db.Users.Where(x => x.Id == 1).ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, users);
    }

    [Test]
    public async Task WhereVariable()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var name = "Bob";
        var users = await _db.Users.Where(x => x.Name == name).ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, users);
    }

    [Test]
    public async Task WhereLessThan()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var users = await _db.Users.Where(x => x.Id < 2).ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(1, "Bob", 25) }, users);
    }

    [Test]
    public async Task WhereGreaterThan()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var users = await _db.Users.Where(x => x.Id > 1).ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(2, "Sara", 25) }, users);
    }

    [Test]
    public async Task SelectScalars()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var userIds = await _db.Users.Select(x => x.Id).ToArrayAsync();
        CollectionAssert.AreEqual(new[] { 1, 2 }, userIds);
    }

    [Test]
    public async Task SelectWhere()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var userIds = await _db.Users
            .Where(x => x.Name == "Bob")
            .Select(x => x.Name)
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { "Bob" }, userIds);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbOptions options)
            : base(options)
        {
            Users = new Table<User>(options);
        }

        public Table<User> Users { get; }
    }

    private record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age
    );
}

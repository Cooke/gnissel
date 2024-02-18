#region

using System.ComponentModel.DataAnnotations.Schema;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.PlusPlus;
using Cooke.Gnissel.Utils;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test.PlusPlus;

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

                    create table devices
                    (
                        name text,
                        user_id  integer references users(id)
                    );

                    create table timestamps
                    (
                        stamp1 timestamp with time zone
                    );
                """
            )
            .ExecuteNonQueryAsync();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dataSource.CreateCommand("DROP TABLE devices").ExecuteNonQuery();
        _dataSource.CreateCommand("DROP TABLE users").ExecuteNonQuery();
        _dataSource.CreateCommand("DROP TABLE timestamps").ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE devices RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE timestamps RESTART IDENTITY CASCADE").ExecuteNonQuery();
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
    public async Task FirstOrDefaultOneMatch()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id > 1);
        Assert.That(user, Is.EqualTo(new User(2, "Sara", 25)));
    }

    [Test]
    public async Task FirstOrDefaultOfTwoMatching()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id > 0);
        Assert.That(user, Is.EqualTo(new User(1, "Bob", 25)));
    }

    [Test]
    public async Task FirstOrDefaultOfNoMatching()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id > 9999);
        Assert.That(user, Is.Null);
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
    public async Task SelectAnonymous()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var users = await _db.Users.Select(x => new { x.Id, TheName = x.Name }).ToArrayAsync();
        CollectionAssert.AreEqual(
            new[] { (1, "Bob"), (2, "Sara") },
            users.Select(x => (x.Id, x.TheName))
        );
    }

    [Test]
    public async Task SelectRecord()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        var users = await _db.Users.Select(x => new User(x.Id, x.Name, x.Age)).ToArrayAsync();
        CollectionAssert.AreEqual(
            new[] { (1, "Bob"), (2, "Sara") },
            users.Select(x => (x.Id, x.Name))
        );
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

    [Test]
    public async Task Delete()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Sara", 25));
        await _db.Users.Delete(x => x.Name == "Bob");
        var userIds = await _db.Users.Select(x => x.Name).ToArrayAsync();
        CollectionAssert.AreEqual(new[] { "Sara" }, userIds);
    }

    [Test]
    public async Task Update()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Update(
            x => x.Name == "Bob",
            op => op.Set(x => x.Name, "Big Bob").Set(x => x.Age, x => x.Age + 1)
        );
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == 1);
        Assert.That(user, Is.EqualTo(new User(1, "Big Bob", 26)));
    }

    [Test]
    public async Task InsertUpdateDateTime()
    {
        var firstStamp = DateTime.UtcNow;
        await _db.Timestamps.Insert(new Timestamp(firstStamp));
        await _db.Timestamps.Update(x => true, op => op.Set(x => x.Stamp1, DateTime.UtcNow));
        var stamp = await _db.Timestamps.FirstAsync(x => true);
        Assert.That(stamp.Stamp1, Is.GreaterThan(firstStamp));
    }

    [Test]
    public async Task Join()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Devices.Insert(new Device(1, "Bobs device"));
        var users = await _db.Users.Join(_db.Devices, (u, d) => u.Id == d.UserId).ToArrayAsync();
        var (user, device) = users.Single();
        Assert.That(user, Is.EqualTo(new User(1, "Bob", 25)));
        Assert.That(device, Is.EqualTo(new Device(1, "Bobs device")));
    }

    [Test]
    public async Task JoinJoin()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Users.Insert(new User(0, "Alice", 25));
        await _db.Devices.Insert(new Device(1, "Bobs device"));
        var users = await _db.Users
            .Join(_db.Devices, (u, d) => u.Id == d.UserId)
            .Join(_db.Users, (u1, _, u2) => u1.Age == u2.Age && u1.Name != u2.Name)
            .ToArrayAsync();
        var (user1, device, user2) = users.Single();
        Assert.That(user1, Is.EqualTo(new User(1, "Bob", 25)));
        Assert.That(user2, Is.EqualTo(new User(2, "Alice", 25)));
        Assert.That(device, Is.EqualTo(new Device(1, "Bobs device")));
    }

    [Test]
    public async Task JoinFirst()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Devices.Insert(new Device(1, "Bobs device"));
        var (user, device) = await _db.Users
            .Join(_db.Devices, (u, d) => u.Id == d.UserId)
            .FirstAsync();
        Assert.That(user, Is.EqualTo(new User(1, "Bob", 25)));
        Assert.That(device, Is.EqualTo(new Device(1, "Bobs device")));
    }

    [Test]
    public async Task JoinFirstPredicate()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        await _db.Devices.Insert(new Device(1, "Bobs device"));
        var (user, device) = await _db.Users
            .Join(_db.Devices, (u, d) => u.Id == d.UserId)
            .FirstAsync((u, d) => u.Name == "Bob" && d.Name == "Bobs device");
        Assert.That(user, Is.EqualTo(new User(1, "Bob", 25)));
        Assert.That(device, Is.EqualTo(new Device(1, "Bobs device")));
    }

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
        public Table<Device> Devices { get; } = new(options);

        public Table<Timestamp> Timestamps { get; } = new(options);
    }

    private record User(
        [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)] int Id,
        string Name,
        int Age
    );

    private record Device(int UserId, string Name);

    private record Timestamp(DateTime Stamp1) { }
}

using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed.Test.Fixtures;
using Xunit.Abstractions;

namespace Cooke.Gnissel.Typed.Test;

[Collection("Database collection")]
public partial class JoinTests : IDisposable
{
    private readonly TestDbContext db;

    public JoinTests(DatabaseFixture databaseFixture, ITestOutputHelper testOutputHelper)
    {
        databaseFixture.SetOutputHelper(testOutputHelper);
        db = new TestDbContext(
            new(new NpgsqlDbAdapter(databaseFixture.DataSourceBuilder.Build()), new DbMappers())
        );
        db.NonQuery(
                $"""
                    create table users
                    (
                        id   integer primary key,
                        name text,
                        age  integer
                    );

                    create table devices
                    (
                        id   text primary key,
                        name text,
                        user_id integer
                    );

                    create table device_keys
                       (
                           device_id text,
                           key text
                       );
                """
            )
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose()
    {
        db.Batch(
                db.NonQuery($"DROP TABLE device_keys"),
                db.NonQuery($"DROP TABLE devices"),
                db.NonQuery($"DROP TABLE users")
            )
            .GetAwaiter()
            .GetResult();
    }

    [Fact]
    public async Task Join()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);
        var sarasDevice = new Device("b", "Sara's device", 2);

        await db.Users.Insert(bob);
        await db.Users.Insert(sara);
        await db.Devices.Insert(bobsDevice);
        await db.Devices.Insert(sarasDevice);

        var usersDevices = await db
            .Users.Join(db.Devices, (u, d) => u.Id == d.UserId)
            .ToArrayAsync();

        Assert.Equal([(bob, bobsDevice), (sara, sarasDevice)], usersDevices);
    }

    [Fact]
    public async Task LeftJoin()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);

        await db.Users.Insert(bob, sara);
        await db.Devices.Insert(bobsDevice);

        var usersDevices = await db
            .Users.LeftJoin(db.Devices, (u, d) => u.Id == d.UserId)
            .ToArrayAsync();

        Assert.Equal([(bob, bobsDevice), (sara, null)], usersDevices);
    }

    [Fact]
    public async Task RightJoin()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);

        await db.Users.Insert(bob, sara);
        await db.Devices.Insert(bobsDevice);

        var usersDevices = await db
            .Devices.RightJoin(db.Users, (d, u) => u.Id == d.UserId)
            .ToArrayAsync();

        Assert.Equal([(bobsDevice, bob), (null, sara)], usersDevices);
    }

    [Fact]
    public async Task FullJoin()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);
        var aliceDevice = new Device("c", "Alice's device", 3);

        await db.Users.Insert(bob, sara);
        await db.Devices.Insert(bobsDevice, aliceDevice);

        var usersDevices = await db
            .Users.FullJoin(db.Devices, (u, d) => u.Id == d.UserId)
            .ToArrayAsync();

        Assert.Equal([(bob, bobsDevice), (null, aliceDevice), (sara, null)], usersDevices);
    }

    [Fact]
    public async Task CrossJoin()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);
        var aliceDevice = new Device("c", "Alice's device", 3);

        await db.Users.Insert(bob, sara);
        await db.Devices.Insert(bobsDevice, aliceDevice);

        var usersDevices = await db.Users.CrossJoin(db.Devices).ToArrayAsync();

        Assert.Equal(
            [(bob, bobsDevice), (bob, aliceDevice), (sara, bobsDevice), (sara, aliceDevice)],
            usersDevices
        );
    }

    [Fact]
    public async Task LeftJoinLeftJoin()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);

        await db.Users.Insert(bob);
        await db.Users.Insert(sara);
        await db.Devices.Insert(bobsDevice);

        var usersDevices = await db
            .Users.LeftJoin(db.Devices, (u, d) => u.Id == d.UserId)
            .LeftJoin(db.Devices, (u, d1, d2) => d1!.Id == d2.Id)
            .ToArrayAsync();

        Assert.Equal([(bob, bobsDevice, bobsDevice), (sara, null, null)], usersDevices);
    }

    [Fact]
    public async Task JoinWhere()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);
        var sarasDevice = new Device("b", "Sara's device", 2);

        await db.Users.Insert(bob);
        await db.Users.Insert(sara);
        await db.Devices.Insert(bobsDevice);
        await db.Devices.Insert(sarasDevice);

        var usersDevices = await db
            .Users.Join(db.Devices, (u, d) => u.Id == d.UserId)
            .Where((u, d) => d.UserId == 2 || d.UserId == 1)
            .ToArrayAsync();

        Assert.Equal([(bob, bobsDevice), (sara, sarasDevice)], usersDevices);
    }

    [Fact]
    public async Task SelfJoin()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);

        await db.Users.Insert(bob);
        await db.Users.Insert(sara);

        var usersDevices = await db.Users.Join(db.Users, (u1, u2) => u1.Id == u2.Id).ToArrayAsync();

        Assert.Equal([(bob, bob), (sara, sara)], usersDevices);
    }

    [Fact]
    public async Task JoinJoin()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);
        var bobsDeviceKey = new DeviceKey("a", "asdf");
        var sarasDevice = new Device("b", "Sara's device", 2);
        var sarasDeviceKey = new DeviceKey("b", "asdf");

        await db.Users.Insert(bob);
        await db.Users.Insert(sara);
        await db.Devices.Insert(bobsDevice);
        await db.Devices.Insert(sarasDevice);
        await db.DeviceKeys.Insert(bobsDeviceKey);
        await db.DeviceKeys.Insert(sarasDeviceKey);

        var usersDevices = await db
            .Users.Join(db.Devices, (u, d) => u.Id == d.UserId)
            .Join(db.DeviceKeys, (_, d, k) => d.Id == k.DeviceId)
            .ToArrayAsync();

        Assert.Equal(
            [(bob, bobsDevice, bobsDeviceKey), (sara, sarasDevice, sarasDeviceKey)],
            usersDevices
        );
    }

    [Fact]
    public async Task JoinJoinWhere()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);
        var bobsDeviceKey = new DeviceKey("a", "asdf");
        var sarasDevice = new Device("b", "Sara's device", 2);
        var sarasDeviceKey = new DeviceKey("b", "asdf");

        await db.Users.Insert(bob);
        await db.Users.Insert(sara);
        await db.Devices.Insert(bobsDevice);
        await db.Devices.Insert(sarasDevice);
        await db.DeviceKeys.Insert(bobsDeviceKey);
        await db.DeviceKeys.Insert(sarasDeviceKey);

        var usersDevices = await db
            .Users.Join(db.Devices, (u, d) => u.Id == d.UserId)
            .Join(db.DeviceKeys, (u, d, k) => d.Id == k.DeviceId)
            .Where((u, d, k) => u.Name == "Sara")
            .ToArrayAsync();

        Assert.Equal([(sara, sarasDevice, sarasDeviceKey)], usersDevices);
    }

    [Fact]
    public async Task JoinJoinWherePartial()
    {
        var bob = new User(1, "Bob", 25);
        var sara = new User(2, "Sara", 25);
        var bobsDevice = new Device("a", "Bob's device", 1);
        var bobsDeviceKey = new DeviceKey("a", "asdf");
        var sarasDevice = new Device("b", "Sara's device", 2);
        var sarasDeviceKey = new DeviceKey("b", "asdf");

        await db.Users.Insert(bob);
        await db.Users.Insert(sara);
        await db.Devices.Insert(bobsDevice);
        await db.Devices.Insert(sarasDevice);
        await db.DeviceKeys.Insert(bobsDeviceKey);
        await db.DeviceKeys.Insert(sarasDeviceKey);

        var usersDevices = await db
            .Users.Join(db.Devices, (u, d) => u.Id == d.UserId)
            .Join(db.DeviceKeys, (u, d, k) => d.Id == k.DeviceId)
            .Where((u) => u.Name == "Sara")
            .ToArrayAsync();

        Assert.Equal([(sara, sarasDevice, sarasDeviceKey)], usersDevices);

        var usersDevices2 = await db
            .Users.Join(db.Devices, (u, d) => u.Id == d.UserId)
            .Join(db.DeviceKeys, (u, d, k) => d.Id == k.DeviceId)
            .Where((u, _) => u.Name == "Sara")
            .ToArrayAsync();

        Assert.Equal([(sara, sarasDevice, sarasDeviceKey)], usersDevices2);
    }

    [Fact]
    public async Task JoinFirst()
    {
        var bob = new User(1, "Bob", 25);
        var bobsDevice1 = new Device("a", "Bob's device 1", 1);
        var bobsDevice2 = new Device("b", "Bob's device 2", 1);

        await db.Users.Insert(bob);
        await db.Devices.Insert(bobsDevice1);
        await db.Devices.Insert(bobsDevice2);

        var usersDevices = await db.Users.Join(db.Devices, (u, d) => u.Id == d.UserId).First();

        Assert.Equal((bob, bobsDevice1), usersDevices);
    }

    [Fact]
    public async Task JoinFirstPredicate()
    {
        var bob = new User(1, "Bob", 25);
        var bobsDevice1 = new Device("a", "Bob's device 1", 1);
        var bobsDevice2 = new Device("b", "Bob's device 2", 1);

        await db.Users.Insert(bob);
        await db.Devices.Insert(bobsDevice1);
        await db.Devices.Insert(bobsDevice2);

        var usersDevices = await db
            .Users.Join(db.Devices, (u, d) => u.Id == d.UserId)
            .First((u, d) => d.Id == "b");

        Assert.Equal((bob, bobsDevice2), usersDevices);
    }

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);

        public Table<Device> Devices { get; } = new(options);

        public Table<DeviceKey> DeviceKeys { get; } = new(options);
    }

    private record User(int Id, string Name, int Age);

    private record Device(string Id, string Name, int UserId);

    private record DeviceKey(string DeviceId, string Key);

    [DbMappers]
    private partial class DbMappers;
}

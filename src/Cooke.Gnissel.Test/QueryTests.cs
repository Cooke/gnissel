using System.Data;
using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed;
using Npgsql;

// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedPositionalProperty.Local


namespace Cooke.Gnissel.Test;

public partial class MappingTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _db = new TestDbContext(new DbOptions(new NpgsqlDbAdapter(_dataSource), new Mappers()));

        await _dataSource
            .CreateCommand(
                """
                    create table users
                    (
                        id   integer primary key,
                        name text NOT NULL,
                        age  integer,
                        description text
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
        _dataSource.CreateCommand("DROP TABLE dates").ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _dataSource.CreateCommand("TRUNCATE users RESTART IDENTITY CASCADE").ExecuteNonQuery();
        _dataSource.CreateCommand("TRUNCATE dates RESTART IDENTITY CASCADE").ExecuteNonQuery();
    }

    [Test]
    public async Task ReadSinglePrimitiveField()
    {
        var results = await _db.Query<int>($"SELECT 1").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { 1 }, results);
    }

    [Test]
    public async Task CustomReading()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query(
                $"SELECT * FROM users",
                x => new User(x.GetInt32(0), x.GetString(x.GetOrdinal("name")), x.GetInt32("age"))
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(0, "Bob", 25) }, results);
    }

    [Test]
    public async Task ReadClassWithConstructor()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new User(0, "Bob", 25) }, results);
    }

    [Test]
    public async Task ReadClassWithConstructorParameterColumnOrderMismatch()
    {
        await _db.Users.Insert(new User(1, "Bob", 25));
        var results = await _db.Query<UserWithParametersInDifferentOrder>($"SELECT * FROM users")
            .ToArrayAsync();
        CollectionAssert.AreEqual(
            new[] { new UserWithParametersInDifferentOrder(25, 1, "Bob") },
            results
        );
    }

    [Test]
    public async Task ReadTuple()
    {
        await _db.Users.Insert(new User(1, "Bob", 25));
        var results = await _db.Query<(int, string, int)>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { (1, "Bob", 25) }, results);
    }

    [Test]
    public async Task ReadDateTimes()
    {
        DateTime withTimeZone = new DateTime(2023, 11, 7, 19, 4, 01, DateTimeKind.Utc);
        DateTime withoutTimeZone = new DateTime(2023, 11, 7, 19, 4, 01, DateTimeKind.Local);
        await _db.NonQuery(
            $"INSERT INTO dates (timestamp_with_timezone, timestamp_without_timezone) VALUES ({withTimeZone}, {withoutTimeZone})"
        );
        var results = await _db.Query<(DateTime WithTimezone, DateTime WithoutTimezone)>(
                $"SELECT timestamp_with_timezone, timestamp_without_timezone FROM dates"
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { (withTimeZone, withoutTimeZone) }, results);
    }

    [Test]
    public async Task ReadNullClass()
    {
        var results = await _db.Query<User?>(
                $"SELECT null as id, null as name, null as age, null as description"
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(new User?[] { null }, results);
    }

    [Test]
    public async Task ReadNullValueType()
    {
        var results = await _db.Query<TimeSpan?>($"SELECT null::int").ToArrayAsync();
        CollectionAssert.AreEqual(new TimeSpan?[] { null }, results);
    }

    [Test]
    public async Task NullComplexInPositionalType()
    {
        var results = await _db.Query<(User, User, User?)>(
                $"SELECT 1 as id, 'Bob' as name, 30 as age, NULL as description, 2 as id, 'Sara' as name, 25 as age, NULL as description, null as id, null as name, null as age, null as description"
            )
            .ToArrayAsync();
        CollectionAssert.AreEqual(
            new (User, User, User?)[] { (new User(1, "Bob", 30), new User(2, "Sara", 25), null) },
            results
        );
    }

    [Test]
    public async Task ComplexTypeWithTypedPrimitiveMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<UserWithTypedPrimitives>($"SELECT name, age FROM users")
            .ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new UserWithTypedPrimitives(new("Bob"), 25) }, results);
    }

    [Test]
    public async Task WrappedPrimitiveMapping()
    {
        await _db.Users.Insert(new User(0, "Bob", 25));
        var results = await _db.Query<Name>($"SELECT name FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { new Name("Bob") }, results);
    }

    [Test]
    public async Task WrappedNullablePrimitiveMapping()
    {
        var results = await _db.Query<Name?>($"SELECT NULL").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { (Name?)null }, results);
    }

    [Test]
    public async Task EnumMapping()
    {
        await _db.NonQuery($"INSERT INTO users (id, name) VALUES (0, {UserName.Bob})");
        var results = await _db.Query<UserName>($"SELECT name FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { UserName.Bob }, results);
    }

    [Test]
    public async Task MapPropertyInAdditionToConstructor()
    {
        var user = await _db.QuerySingle<UserWithProp>($"SELECT 1 as id, 'Bob' as desc");
        Assert.That(user, Is.EqualTo(new UserWithProp(1) { Desc = "Bob" }));
    }

    private record UserWithProp(int Id)
    {
        public required string Desc { get; init; }
    }

    private enum UserName
    {
        Bob,
    }

    private record User(int Id, string Name, int Age, string? Description = null)
    {
        // This constructor should be ignored when mapping, longest constructor is used
        public User(int id)
            : this(id, "Default", 12) { }

        // Should be ignored when mapping since private setter
        public string DescriptionOrName { get; private init; } = Description ?? Name;
    };

    private record UserWithParametersInDifferentOrder(int Age, int Id, string Name);

    private record UserWithTypedPrimitives(Name Name, int Age);

    private record Name(string Value);

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);
    }

    [DbMappers(
        EnumMappingTechnique = MappingTechnique.AsString,
        NamingConvention = NamingConvention.SnakeCase
    )]
    private partial class Mappers;
}

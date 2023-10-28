#region

using System.Text.Json;
using System.Text.Json.Serialization;
using Cooke.Gnissel.Npgsql;
using Npgsql;
using Npgsql.Internal;

#endregion

namespace Cooke.Gnissel.Test;

public class MappingJsonTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSource;
    private TestDbContext _db;

    [OneTimeSetUp]
    public async Task Setup()
    {
        // var builder = new NpgsqlDataSourceBuilder(_dataSource.ConnectionString);

        // var jsonTypeInfoResolver = typeof(NpgsqlCommand).Assembly.GetType(
        //     "Npgsql.Internal.Resolvers.JsonTypeInfoResolver"
        // )!;
        // var typeInfoResolver = (IPgTypeInfoResolver)Activator.CreateInstance(jsonTypeInfoResolver)!;
        // builder.AddTypeInfoResolver(typeInfoResolver);
        _db = new TestDbContext(new DbOptions(new NpgsqlDbAdapter(_dataSource)));

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
    public async Task NestedJsonMapping()
    {
        var inserted = new User(
            0,
            "Bob",
            25,
            new UserData("bob", 1, UserRole.User, TimeSpan.FromHours(1), GameClass.Warrior)
        );
        await _db.Users.Insert(inserted);
        var results = await _db.Query<User>($"SELECT * FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { inserted }, results);
    }

    [Test]
    public async Task DirectJsonMapping()
    {
        var inserted = new User(
            0,
            "Bob",
            25,
            new UserData("bob", 1, UserRole.User, TimeSpan.FromHours(1), GameClass.Healer)
        );
        await _db.Users.Insert(inserted);
        var results = await _db.Query<UserData>($"SELECT data FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { inserted.Data }, results);
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

    private record User(int Id, string Name, int Age, UserData Data);

    [DbType("jsonb")]
    private record UserData(
        string Username,
        int Level,
        UserRole Role,
        TimeSpan PlayTime,
        GameClass Class
    );

    public class GameClass
    {
        public static readonly GameClass Warrior = new GameClass("Warrior");
        public static readonly GameClass Healer = new GameClass("Healer");

        public string Name { get; }

        private GameClass(string name)
        {
            Name = name;
        }
    }

    public class GameClassConverter : JsonConverter<GameClass>
    {
        public override GameClass? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            return reader.GetString() switch
            {
                "Warrior" => GameClass.Warrior,
                "Healer" => GameClass.Healer,
                _ => throw new Exception("Invalid class")
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            GameClass value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.Name);
        }
    }

    internal enum UserRole
    {
        Admin,
        User
    }

    public record Device(string Id, string Name, int UserId);
}

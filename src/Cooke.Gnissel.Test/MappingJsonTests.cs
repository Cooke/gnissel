#region

using System.Text.Json;
using System.Text.Json.Serialization;
using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Typed;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public partial class MappingJsonTests
{
    private NpgsqlDataSource _dataSource = null!;
    private TestDbContext _db = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _dataSource = Fixture
            .DataSourceBuilder.ConfigureJsonOptions(
                new JsonSerializerOptions { Converters = { new GameClassConverter() } }
            )
            .EnableDynamicJson()
            .Build();
        var adapter = new NpgsqlDbAdapter(_dataSource);
        _db = new TestDbContext(
            new(
                adapter,
                new DbMappers()
                {
                    Readers = new DbMappers.DbReaders()
                    {
                        MappingJsonTestsGameClassReader = new ObjectReader<GameClass?>(
                            (reader, ordinalReader) =>
                                GameClass.TryParse(
                                    reader.GetValueOrNull<string>(ordinalReader.Read())
                                ),
                            () => [new NextOrdinalReadDescriptor()]
                        ),
                    },
                }
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
        _dataSource
            .CreateCommand(
                "INSERT INTO users(id, name, age, data) VALUES(2, 'Bob', 25, '{\"Username\": \"bob\", \"Level\": 1, \"Role\": 1, \"PlayTime\": \"01:00:00\", \"Class\": \"Healer\"}')"
            )
            .ExecuteNonQuery();
        var results = await _db.Query<UserData>($"SELECT data FROM users").ToArrayAsync();
        CollectionAssert.AreEqual(
            new[]
            {
                new UserData("bob", 1, UserRole.User, TimeSpan.FromHours(1), GameClass.Healer),
            },
            results
        );
    }

    private class TestDbContext(DbOptions options) : DbContext(options)
    {
        public Table<User> Users { get; } = new(options);

        public Table<Device> Devices { get; } = new(options);
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

    // TODO move to JsonSerializerOptions when upgrading to npgsql 8
    [JsonConverter(typeof(GameClassConverter))]
    public class GameClass
    {
        public static readonly GameClass Warrior = new GameClass("Warrior");
        public static readonly GameClass Healer = new GameClass("Healer");

        public string Name { get; }

        private GameClass(string name)
        {
            Name = name;
        }

        public static GameClass? TryParse(string? str) =>
            str switch
            {
                "Warrior" => Warrior,
                "Healer" => Healer,
                _ => null,
            };
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
                _ => throw new Exception("Invalid class"),
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
        User,
    }

    public record Device(string Id, string Name, int UserId);

    [DbMappers]
    public partial class DbMappers;
}

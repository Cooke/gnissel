#region

using System.Text.Json;
using System.Text.Json.Serialization;
using Cooke.Gnissel.Npgsql;
using Npgsql;

// ReSharper disable NotAccessedPositionalProperty.Local

#endregion

namespace Cooke.Gnissel.Test;

public partial class ReadJsonTests
{
    private NpgsqlDataSource _dataSource = null!;
    private DbContext _db = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _dataSource = Fixture
            .DataSourceBuilder.ConfigureJsonOptions(
                new JsonSerializerOptions { Converters = { new GameClassConverter() } }
            )
            .EnableDynamicJson()
            .Build();
        _db = new DbContext(new(new NpgsqlDbAdapter(_dataSource), new DbMappers()));
    }

    [Test]
    public async Task ReadJsonAsIs()
    {
        var result = await _db.QuerySingle<UserData>(
            $$"""SELECT '{"Username": "bob", "Level": 1, "Role": 1, "PlayTime": "01:00:00", "Class": "Healer"}'::jsonb"""
        );
        Assert.That(
            result,
            Is.EqualTo(
                new UserData("bob", 1, UserRole.User, TimeSpan.FromHours(1), GameClass.Healer)
            )
        );
    }

    [Test]
    public async Task ReadJsonAsIsInsideClass()
    {
        var result = await _db.QuerySingle<User>(
            $$"""SELECT 1 as "Id", '{"Username": "bob", "Level": 1, "Role": 1, "PlayTime": "01:00:00", "Class": "Warrior"}'::jsonb as "Data" """
        );
        Assert.That(
            result,
            Is.EqualTo(
                new User(
                    1,
                    new UserData("bob", 1, UserRole.User, TimeSpan.FromHours(1), GameClass.Warrior)
                )
            )
        );
    }

    private record User(int Id, UserData Data);

    [DbMap(Technique = MappingTechnique.AsIs)]
    private record UserData(
        string Username,
        int Level,
        UserRole Role,
        TimeSpan PlayTime,
        GameClass Class
    );

    // TODO move to JsonSerializerOptions when upgrading to npgsql 8
    [JsonConverter(typeof(GameClassConverter))]
    private class GameClass
    {
        public static readonly GameClass Warrior = new GameClass("Warrior");
        public static readonly GameClass Healer = new GameClass("Healer");

        public string Name { get; }

        private GameClass(string name) => Name = name;
    }

    private class GameClassConverter : JsonConverter<GameClass>
    {
        public override GameClass Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) =>
            reader.GetString() switch
            {
                "Warrior" => GameClass.Warrior,
                "Healer" => GameClass.Healer,
                _ => throw new Exception("Invalid class"),
            };

        public override void Write(
            Utf8JsonWriter writer,
            GameClass value,
            JsonSerializerOptions options
        ) => writer.WriteStringValue(value.Name);
    }

    internal enum UserRole
    {
        Admin,
        User,
    }

    [DbMappers]
    private partial class DbMappers;
}

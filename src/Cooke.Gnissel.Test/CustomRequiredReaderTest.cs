#region

using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Npgsql;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public partial class CustomRequiredReaderTest
{
    private NpgsqlDataSource _dataSource = null!;
    private DbContext _db;

    [OneTimeSetUp]
    public void Setup()
    {
        _dataSource = Fixture.DataSourceBuilder.Build();
        var adapter = new NpgsqlDbAdapter(_dataSource);
        var nameProvider = new SnakeCaseDbNameProvider();
        _db = new DbContext(
            new(
                adapter,
                new DbMappers(nameProvider)
                {
                    Readers = new DbMappers.DbReaders(nameProvider)
                    {
                        CustomRequiredReaderTestGameClassReader = new ObjectReader<GameClass?>(
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
    }

    [Test]
    public async Task Read()
    {
        var results = await _db.Query<GameClass>($"SELECT 'Warrior'").ToArrayAsync();
        CollectionAssert.AreEqual(new[] { GameClass.Warrior }, results);
    }

    private class GameClass
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

    [DbMappers]
    private partial class DbMappers;
}

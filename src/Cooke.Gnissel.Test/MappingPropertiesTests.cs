#region

using Cooke.Gnissel.Npgsql;
using Gnissel.SourceGeneration;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public partial class MappingPropertiesTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private DbContext _db;

    [OneTimeSetUp]
    public void Setup()
    {
        _db = new DbContext(new NpgsqlDbAdapter(_dataSource), ObjectReaders.AllDescriptors);
    }

    [Test]
    public async Task MapPropertyInAdditionToConstructor()
    {
        var user = await _db.QuerySingle<User>($"SELECT 1 as id, 'Bob' as name");
        Assert.That(user, Is.EqualTo(new User(1) { Name = "Bob" }));
    }

    public record User(int Id)
    {
        public required string Name { get; init; }
    }
}

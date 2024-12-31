#region

using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Services.Implementations;
using Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public class MappingProperiesTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private DbContext _db;

    [OneTimeSetUp]
    public void Setup()
    {
        var adapter = new NpgsqlDbAdapter(_dataSource);
        _db = new DbContext(new(adapter, new ExpressionObjectReaderProvider(adapter)));
    }

    [Test]
    public async Task MapPropertyInAdditionToConstructor()
    {
        var user = await _db.QuerySingle<User>($"SELECT 1 as id, 'Bob' as name");
        Assert.That(user, Is.EqualTo(new User(1) { Name = "Bob" }));
    }

    private record User(int Id)
    {
        public required string Name { get; init; }
    }
}

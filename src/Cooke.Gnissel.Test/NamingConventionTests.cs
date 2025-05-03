#region

using Cooke.Gnissel.Npgsql;

#endregion

namespace Cooke.Gnissel.Test;

public partial class NamingConventionTests
{
    private NpgsqlDbAdapter _adapter;

    [OneTimeSetUp]
    public void Setup()
    {
        _adapter = new NpgsqlDbAdapter(Fixture.DataSourceBuilder.Build());
    }

    [Test]
    public async Task ReadAsIs()
    {
        var db = new DbContext(new(_adapter, new Mapper(new DefaultDbNameProvider())));
        var user = await db.QuerySingle<User>(
            $"SELECT '1' as \"Id\", 'Joe' as \"FirstName\", 25 as \"Age\", 'Queenstreet' as \"StreetName\", 'New York' as \"City\""
        );
        Assert.Multiple(() =>
        {
            Assert.That(user.Id, Is.EqualTo("1"));
            Assert.That(user.FirstName, Is.EqualTo("Joe"));
            Assert.That(user.Age, Is.EqualTo(25));
            Assert.That(user.Address.StreetName, Is.EqualTo("Queenstreet"));
            Assert.That(user.Address.City, Is.EqualTo("New York"));
        });
    }

    [Test]
    public async Task ReadSnakeCase()
    {
        var db = new DbContext(new(_adapter, new Mapper(new SnakeCaseDbNameProvider())));
        var user = await db.QuerySingle<User>(
            $"SELECT '1' as \"id\", 'Joe' as \"first_name\", 25 as \"age\", 'Queenstreet' as \"street_name\", 'New York' as \"city\""
        );
        Assert.Multiple(() =>
        {
            Assert.That(user.Id, Is.EqualTo("1"));
            Assert.That(user.FirstName, Is.EqualTo("Joe"));
            Assert.That(user.Age, Is.EqualTo(25));
            Assert.That(user.Address.StreetName, Is.EqualTo("Queenstreet"));
            Assert.That(user.Address.City, Is.EqualTo("New York"));
        });
    }

    private record User(string Id, string FirstName, int Age, UserAddress Address);

    private record UserAddress(string StreetName, string City);

    [DbMappers]
    private partial class Mapper;
}

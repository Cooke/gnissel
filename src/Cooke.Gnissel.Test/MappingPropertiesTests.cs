#region

using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using Cooke.Gnissel.AsyncEnumerable;
using Cooke.Gnissel.Converters;
using Cooke.Gnissel.Npgsql;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Typed;
using Cooke.Gnissel.Utils;
using Npgsql;
using Org.BouncyCastle.Asn1.X509;

#endregion

namespace Cooke.Gnissel.Test;

public class MappingProperiesTests
{
    private readonly NpgsqlDataSource _dataSource = Fixture.DataSourceBuilder.Build();
    private DbContext _db;

    [OneTimeSetUp]
    public void Setup()
    {
        _db = new DbContext(new(new NpgsqlDbAdapter(_dataSource)));
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

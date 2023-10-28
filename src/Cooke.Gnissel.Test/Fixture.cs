#region

using System.Text.Json;
using Npgsql;
using Testcontainers.PostgreSql;

#endregion

namespace Cooke.Gnissel.Test;

[SetUpFixture]
public class Fixture
{
    public static NpgsqlDataSource DataSource;

    private PostgreSqlContainer _postgres;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();
        var jsonOptions = new JsonSerializerOptions
        {
            Converters = { new MappingJsonTests.GameClassConverter() }
        };
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            _postgres.GetConnectionString()
        ).EnableDynamicJsonMappings(jsonOptions);
        DataSource = dataSourceBuilder.Build();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _postgres.DisposeAsync();
    }
}

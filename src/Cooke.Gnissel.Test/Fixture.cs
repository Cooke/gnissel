using System.ComponentModel.DataAnnotations.Schema;
using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;

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
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
        DataSource = dataSourceBuilder.Build();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _postgres.DisposeAsync();
    }
}

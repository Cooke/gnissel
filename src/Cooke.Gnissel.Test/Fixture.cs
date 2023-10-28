#region

using Npgsql;
using Testcontainers.PostgreSql;

#endregion

namespace Cooke.Gnissel.Test;

[SetUpFixture]
public class Fixture
{
    public static NpgsqlDataSourceBuilder DataSourceBuilder = null!;

    private PostgreSqlContainer _postgres = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();
        DataSourceBuilder = new NpgsqlDataSourceBuilder(
            new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
            {
                IncludeErrorDetail = true
            }.ConnectionString
        );
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _postgres.DisposeAsync();
    }
}

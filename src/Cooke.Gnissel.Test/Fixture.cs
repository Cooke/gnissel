#region

using Extensions.Logging.NUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var services = new ServiceCollection();
        services.AddLogging(x => x.AddProvider(new NUnitLoggerProvider()));
        var sp = services.BuildServiceProvider();

        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();
        DataSourceBuilder = new NpgsqlDataSourceBuilder(
            new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
            {
                IncludeErrorDetail = true,
            }.ConnectionString
        ).UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _postgres.DisposeAsync();
    }
}

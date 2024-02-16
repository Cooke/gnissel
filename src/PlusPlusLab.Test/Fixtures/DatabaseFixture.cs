#region

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using PlusPlusLab.Test;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

#endregion

namespace Cooke.Gnissel.Test;

public class DatabaseFixture : IDisposable
{
    public readonly NpgsqlDataSourceBuilder _dataSourceBuilder;

    private readonly PostgreSqlContainer _postgres;
    private ITestOutputHelper? _testOutputHelper;

    public DatabaseFixture(IMessageSink messageSink)
    {
        var services = new ServiceCollection();

        services.AddLogging(
            c =>
                c.AddConsole().AddProvider(new TestOutputHelperLogProvider(() => _testOutputHelper))
        );
        var sp = services.BuildServiceProvider();

        _postgres = new PostgreSqlBuilder().Build();
        _postgres.StartAsync().Wait();
        _dataSourceBuilder = new NpgsqlDataSourceBuilder(
            new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
            {
                IncludeErrorDetail = true,
            }.ConnectionString
        ).UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
    }

    public NpgsqlDataSourceBuilder DataSourceBuilder => _dataSourceBuilder;

    public void Dispose()
    {
        _postgres.DisposeAsync().GetAwaiter().GetResult();
    }

    public void SetOutputHelper(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
}

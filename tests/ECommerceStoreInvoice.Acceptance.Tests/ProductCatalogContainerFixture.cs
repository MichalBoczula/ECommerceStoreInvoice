using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace ECommerceStoreInvoice.Acceptance.Tests;

/// <summary>
/// Starts the published Products API and its SQL Server dependency on a private Docker network.
/// The existing acceptance scenarios still use ScenarioProductServiceClient; wiring Orders to
/// this running API is a separate step.
/// </summary>
public sealed class ProductCatalogContainerFixture : IAsyncLifetime
{
    private const string SqlPassword = "yourStrong(!)Password";
    private const ushort SqlPort = 1433;
    private const ushort ApiPort = 8080;

    private readonly INetwork _network = new NetworkBuilder().Build();
    private readonly MsSqlContainer _sql;
    private readonly IContainer _api;

    public ProductCatalogContainerFixture()
    {
        _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPortBinding(SqlPort, true)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SQLCMDUSER", "sa")
            .WithEnvironment("SQLCMDPASSWORD", SqlPassword)
            .WithEnvironment("MSSQL_SA_PASSWORD", SqlPassword)
            .WithNetwork(_network)
            .WithNetworkAliases("product-db")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(SqlPort))
            .Build();

        _api = new ContainerBuilder("mb0101/product-catalog-api:latest")
            .WithNetwork(_network)
            .WithPortBinding(ApiPort, true)
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
            .WithEnvironment(
                "ConnectionStrings__ProductCatalogDb",
                $"Server=product-db;Database=ProductsDb;User Id=sa;Password={SqlPassword};TrustServerCertificate=True")
            .WithEnvironment("Database__ApplyMigrations", "true")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(ApiPort).ForPath("/health/live")))
            .Build();
    }

    public Uri BaseAddress => new($"http://{_api.Hostname}:{_api.GetMappedPublicPort(ApiPort)}/");

    public async Task InitializeAsync()
    {
        try
        {
            await _network.CreateAsync();
            await _sql.StartAsync();
            await WaitForSqlAsync();
            await _api.StartAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        await _sql.DisposeAsync();
        await _network.DisposeAsync();
    }

    private async Task WaitForSqlAsync()
    {
        var connectionString =
            $"Server={_sql.Hostname},{_sql.GetMappedPublicPort(SqlPort)};" +
            $"Database=master;User Id=sa;Password={SqlPassword};" +
            "TrustServerCertificate=True;Encrypt=False;Connection Timeout=5;";

        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();
                return;
            }
            catch (SqlException) when (attempt < 29)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}

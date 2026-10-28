using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace ECommerceStoreInvoice.Acceptance.Tests;

public sealed class ProductCatalogContainerFixture : IAsyncLifetime
{
    // ProductsCatalog 59e1d8f827fac1d1c87cd67eaef884d92a06d1a3, CI 40398686283.
    private const string ProductCatalogImage = "docker.io/mb0101/product-catalog-api@sha256:01e6de37c57f108f843a7fad616b412b84b41c53f7d30e59b20b77889a3207cd";
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

        _api = new ContainerBuilder(ProductCatalogImage)
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

    // Direct test-fixture changes only; callers must own this fixture, never the shared catalogue.
    internal async Task ChangeProductFixtureAsync(Guid productId, string name, string brand, decimal amount, string currency)
    {
        var connectionString = $"Server={_sql.Hostname},{_sql.GetMappedPublicPort(SqlPort)};Database=ProductsDb;" +
            $"User Id=sa;Password={SqlPassword};TrustServerCertificate=True;Encrypt=False;Connection Timeout=5;";
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.TB_MobilePhones SET Name=@name, Brand=@brand, PriceAmount=@amount, PriceCurrency=@currency WHERE Id=@id";
        command.Parameters.AddWithValue("@id", productId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@brand", brand);
        var price = command.Parameters.Add("@amount", System.Data.SqlDbType.Decimal);
        price.Precision = 18;
        price.Scale = 2;
        price.Value = amount;
        command.Parameters.AddWithValue("@currency", currency);
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException("Expected exactly one isolated catalog fixture row.");
    }

    internal Task StopApiAsync() => _api.StopAsync();

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

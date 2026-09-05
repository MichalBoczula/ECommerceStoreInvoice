using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Images;
using Testcontainers.MsSql;

namespace ECommerceStoreInvoice.ExternalProviders.IntegrationTests.Configuration
{
    public class ExternalProvidersApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly INetwork _network;
        private readonly MsSqlContainer _dbContainer;
        private readonly IContainer _apiContainer;

        private string _productApiBaseUrl = string.Empty;

        public ExternalProvidersApplicationFactory()
        {
            _network = new NetworkBuilder()
                .WithName(Guid.NewGuid().ToString("D"))
                .Build();

            _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithNetwork(_network)
                .WithNetworkAliases("product-db")
                .WithPassword("YourStrong@Password123!")
                .Build();

            _apiContainer = new ContainerBuilder("product-catalog-api:latest")
                .WithImagePullPolicy(PullPolicy.Never)
                .WithNetwork(_network)
                .WithPortBinding(8080, true)
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
                .WithEnvironment("ConnectionStrings__ProductCatalogDb", "Server=product-db;Database=ProductsDb;User Id=sa;Password=YourStrong@Password123!;TrustServerCertificate=True")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8080))
                .Build();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseSetting("ExternalServices:ProductApiBaseUrl", _productApiBaseUrl);
        }

        public async Task InitializeAsync()
        {
            await _network.CreateAsync();
            await _dbContainer.StartAsync();
            await _apiContainer.StartAsync();

            var host = _apiContainer.Hostname;
            var port = _apiContainer.GetMappedPublicPort(8080);

            _productApiBaseUrl = $"http://{host}:{port}";
        }

        public new async Task DisposeAsync()
        {
            await _apiContainer.DisposeAsync();
            await _dbContainer.DisposeAsync();
            await _network.DeleteAsync();
        }
    }
}
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Testcontainers.MongoDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ECommerceStoreInvoice.Acceptance.Tests
{
    public class ApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string Database = "IntegrationTestDb";
        private const string Username = "root";
        private const string Password = "yourStrong(!)Password";
        private const ushort MongoPort = 27017;

        private readonly MongoDbContainer _mongoContainer;
        private string _connectionString = string.Empty;

        public ApplicationFactory()
        {
            _mongoContainer = new MongoDbBuilder()
                .WithImage("mongo:7.0")
                .WithUsername(Username)
                .WithPassword(Password)
                .Build();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {

                services.RemoveAll<IMongoDatabase>();
                services.RemoveAll<MongoDbContext>();
                services.AddScoped<MongoDbContext>(_ =>
                {
                    var settings = new MongoDbSettings
                    {
                        ConnectionString = _connectionString,
                        DatabaseName = Database
                    };
                    return new MongoDbContext(Options.Create(settings));
                });
            });
        }

        public async Task InitializeAsync()
        {
            await _mongoContainer.StartAsync();

            var host = _mongoContainer.Hostname;
            var port = _mongoContainer.GetMappedPublicPort(MongoPort);

            _connectionString =
                $"mongodb://{Username}:{Password}@{host}:{port}/{Database}?authSource=admin";

            using var scope = Services.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<MongoInitializer>();
            await initializer.EnsureIndexesCreatedAsync();
        }

        public new async Task DisposeAsync()
        {
            await _mongoContainer.DisposeAsync();
        }
    }
}
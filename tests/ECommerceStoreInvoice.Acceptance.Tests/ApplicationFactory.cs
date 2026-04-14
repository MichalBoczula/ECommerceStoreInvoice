using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Acceptance.Tests;

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
            .WithImage("mongo:8.0")
            .WithUsername(Username)
            .WithPassword(Password)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var mongoConfiguration = new Dictionary<string, string?>
            {
                ["MongoDbSettings:ConnectionString"] = _connectionString,
                ["MongoDbSettings:DatabaseName"] = Database,
                ["MongoDbSettings:ShoppingCartsCollectionName"] = "shoppingCarts",
                ["MongoDbSettings:OrdersCollectionName"] = "orders",
                ["MongoDbSettings:ProductVersionsCollectionName"] = "productVersions",
                ["MongoDbSettings:InvoicesCollectionName"] = "invoices",
                ["MongoDbSettings:ClientDataVersionsCollectionName"] = "clientDataVersions"
            };

            configBuilder.AddInMemoryCollection(mongoConfiguration);
        });

        builder.ConfigureServices(services =>
        {
            var infrastructureAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "ECommerceStoreInvoice.Infrastructure")
                ?? Assembly.Load("ECommerceStoreInvoice.Infrastructure");

            var mongoDbContextType = infrastructureAssembly.GetType("ECommerceStoreInvoice.Infrastructure.Context.MongoDbContext")
                ?? throw new InvalidOperationException("Could not resolve MongoDbContext type.");

            services.RemoveAll(mongoDbContextType);
            services.AddSingleton(mongoDbContextType, provider => ActivatorUtilities.CreateInstance(provider, mongoDbContextType));
        });
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        var host = _mongoContainer.Hostname;
        var port = _mongoContainer.GetMappedPublicPort(MongoPort);
        _connectionString = $"mongodb://{Username}:{Password}@{host}:{port}/{Database}?authSource=admin";

        using var scope = Services.CreateScope();
        var infrastructureAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ECommerceStoreInvoice.Infrastructure")
            ?? Assembly.Load("ECommerceStoreInvoice.Infrastructure");

        var mongoInitializerType = infrastructureAssembly.GetType("ECommerceStoreInvoice.Infrastructure.Configuration.MongoInitializer")
            ?? throw new InvalidOperationException("Could not resolve MongoInitializer type.");

        var initializer = scope.ServiceProvider.GetRequiredService(mongoInitializerType);
        var initializeAsyncMethod = mongoInitializerType.GetMethod("InitializeAsync")
            ?? throw new InvalidOperationException("Could not resolve InitializeAsync method.");

        var initializationTask = (Task?)initializeAsyncMethod.Invoke(initializer, new object?[] { default(CancellationToken) });
        if (initializationTask is null)
            throw new InvalidOperationException("Mongo initialization did not return a task.");

        await initializationTask;
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }
}

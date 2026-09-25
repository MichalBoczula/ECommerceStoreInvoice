using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Acceptance.Tests;

public class ApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Username = "root";
    private const string Password = "yourStrong(!)Password";

    private static readonly SemaphoreSlim PlaywrightInstallSemaphore = new(1, 1);
    private static bool _playwrightInstalled;

    private static readonly MongoDbContainer SharedMongoContainer = new MongoDbBuilder("mongo:8.0")
        .WithUsername(Username)
        .WithPassword(Password)
        .WithReplicaSet()
        .Build();
    private static readonly Lazy<Task> StartMongo = new(() => SharedMongoContainer.StartAsync());

    private static readonly Lazy<Task<ProductCatalogContainerFixture>> StartProducts = new(async () =>
    {
        var fixture = new ProductCatalogContainerFixture();
        await fixture.InitializeAsync();
        return fixture;
    });

    private readonly bool _useProductCatalog;
    private string? _productCatalogBaseUrl;

    public ApplicationFactory() : this(false) { }

    internal ApplicationFactory(bool useProductCatalog) => _useProductCatalog = useProductCatalog;

    public static async Task DisposeSharedProductsAsync()
    {
        if (StartProducts.IsValueCreated && StartProducts.Value.IsCompletedSuccessfully)
            await (await StartProducts.Value).DisposeAsync();
    }

    private readonly string _database = $"IntegrationTestDb_{Guid.NewGuid():N}";
    private string _connectionString = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        if (_useProductCatalog)
            builder.UseSetting("ExternalServices:ProductCatalog:BaseUrl", _productCatalogBaseUrl!);

        builder.UseSetting("MongoDbSettings:ConnectionString", _connectionString);
        builder.UseSetting("MongoDbSettings:DatabaseName", _database);
        builder.UseSetting("MongoDbSettings:ShoppingCartsCollectionName", "shoppingCarts");
        builder.UseSetting("MongoDbSettings:OrdersCollectionName", "orders");
        builder.UseSetting("MongoDbSettings:ProductVersionsCollectionName", "productVersions");
        builder.UseSetting("MongoDbSettings:InvoicesCollectionName", "invoices");
        builder.UseSetting("MongoDbSettings:ClientDataVersionsCollectionName", "clientDataVersions");

    }

    public async Task InitializeAsync()
    {
        await EnsurePlaywrightInstalledAsync();

        if (_useProductCatalog)
            _productCatalogBaseUrl = (await StartProducts.Value).BaseAddress.ToString();

        await StartMongo.Value;

        _connectionString = SharedMongoContainer.GetConnectionString();

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

        var initializationTask = (Task?)initializeAsyncMethod.Invoke(
            initializer,
            new object?[] { default(CancellationToken) });

        if (initializationTask is null)
        {
            throw new InvalidOperationException("Mongo initialization did not return a task.");
        }

        await initializationTask;
    }

    public new async Task DisposeAsync()
    {
        base.Dispose();
        if (!string.IsNullOrEmpty(_connectionString))
            await new MongoClient(_connectionString).DropDatabaseAsync(_database);
    }

    private static async Task EnsurePlaywrightInstalledAsync()
    {
        if (_playwrightInstalled)
        {
            return;
        }

        await PlaywrightInstallSemaphore.WaitAsync();

        try
        {
            if (_playwrightInstalled)
            {
                return;
            }

            var playwrightScriptPath = FindPlaywrightScriptPath();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"\"{playwrightScriptPath}\" install",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo)
                ?? throw new InvalidOperationException("Could not start Playwright installation process.");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"""
                    Playwright installation failed.

                    Script:
                    {playwrightScriptPath}

                    Exit code:
                    {process.ExitCode}

                    Output:
                    {output}

                    Error:
                    {error}
                    """);
            }

            _playwrightInstalled = true;
        }
        finally
        {
            PlaywrightInstallSemaphore.Release();
        }
    }

    private static string FindPlaywrightScriptPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var playwrightScriptPath = Path.Combine(directory.FullName, "playwright.ps1");

            if (File.Exists(playwrightScriptPath))
            {
                return playwrightScriptPath;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"""
            Could not find playwright.ps1.

            AppContext.BaseDirectory:
            {AppContext.BaseDirectory}

            Make sure the test project references Microsoft.Playwright
            and the project was built before running tests.
            """);
    }
}

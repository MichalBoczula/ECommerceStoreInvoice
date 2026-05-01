using System.Diagnostics;
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

    private static readonly SemaphoreSlim PlaywrightInstallSemaphore = new(1, 1);
    private static bool _playwrightInstalled;

    private readonly MongoDbContainer _mongoContainer;
    private string _connectionString = string.Empty;

    public ApplicationFactory()
    {
        _mongoContainer = new MongoDbBuilder("mongo:8.0")
            .WithUsername(Username)
            .WithPassword(Password)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("MongoDbSettings:ConnectionString", _connectionString);
        builder.UseSetting("MongoDbSettings:DatabaseName", Database);
        builder.UseSetting("MongoDbSettings:ShoppingCartsCollectionName", "shoppingCarts");
        builder.UseSetting("MongoDbSettings:OrdersCollectionName", "orders");
        builder.UseSetting("MongoDbSettings:ProductVersionsCollectionName", "productVersions");
        builder.UseSetting("MongoDbSettings:InvoicesCollectionName", "invoices");
        builder.UseSetting("MongoDbSettings:ClientDataVersionsCollectionName", "clientDataVersions");

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
        await EnsurePlaywrightInstalledAsync();

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
        await _mongoContainer.DisposeAsync();
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
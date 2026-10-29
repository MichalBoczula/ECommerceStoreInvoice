using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using MongoDB.Bson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Products;
using Testcontainers.MongoDb;
using MongoDB.Driver;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;

namespace ECommerceStoreInvoice.Acceptance.Tests;

public partial class ApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Username = "root";
    private const string Password = "yourStrong(!)Password";

    private static readonly SemaphoreSlim PlaywrightInstallSemaphore = new(1, 1);
    private static bool _playwrightInstalled;

    private static readonly Lazy<MongoDbContainer> SharedMongoContainer = new(() => new MongoDbBuilder("mongo:8.0")
        .WithUsername(Username)
        .WithPassword(Password)
        .WithReplicaSet()
        .Build());
    private static readonly Lazy<Task> StartMongo = new(() => SharedMongoContainer.Value.StartAsync());

    private static readonly Lazy<Task<ProductCatalogContainerFixture>> StartProducts = new(async () =>
    {
        var fixture = new ProductCatalogContainerFixture();
        await fixture.InitializeAsync();
        return fixture;
    });

    private readonly bool _useProductCatalog;
    private readonly bool _failPdfOnce;
    private readonly bool _loseCompletionAckOnce;
    private readonly bool _failDownload;
    private readonly CompletionAcknowledgementState _completionAckState = new();
    private readonly PdfFailureState _pdfFailureState = new();
    private string? _productCatalogBaseUrl;
    private readonly bool _isolatedProducts;
    private ProductCatalogContainerFixture? _ownedProducts;
    private int _catalogRequests;
    internal Func<DelegatingHandler>? CatalogEvidenceHandlerFactory { get; set; }
    internal InvoiceReadStorage? ReadStorage { get; set; }
    internal InvoiceReadProbe? ReadProbe { get; set; }
    internal InvoiceGenerationProbe? InvoiceProbe { get; set; }
    internal OrderConcurrencyProbe? OrderProbe { get; set; }
    internal int CatalogRequestCount => Volatile.Read(ref _catalogRequests);
    internal Uri ProductCatalogBaseAddress => new(_productCatalogBaseUrl ?? throw new InvalidOperationException("Products is not enabled."));

    public ApplicationFactory() : this(false) { }

    internal ApplicationFactory(bool useProductCatalog, bool failPdfOnce = false, bool loseCompletionAckOnce = false, bool failDownload = false, bool isolatedProducts = false)
    {
        _useProductCatalog = useProductCatalog;
        _failPdfOnce = failPdfOnce;
        _loseCompletionAckOnce = loseCompletionAckOnce;
        _failDownload = failDownload;
        _isolatedProducts = isolatedProducts;
    }

    public static async Task DisposeSharedProductsAsync()
    {
        if (StartProducts.IsValueCreated && StartProducts.Value.IsCompletedSuccessfully)
            await (await StartProducts.Value).DisposeAsync();
    }

    private readonly string _database = $"IntegrationTestDb_{Guid.NewGuid():N}";
    private string _connectionString = string.Empty;
    private readonly List<string> _downloadFiles = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        if (ReadStorage is not null)
        {
            builder.UseSetting("InvoiceStorage:ConnectionString", ReadStorage.IsBlob ? ReadStorage.ApplicationConnectionString : string.Empty);
            builder.UseSetting("InvoiceStorage:ContainerName", ReadStorage.ContainerName);
        }
        if (_useProductCatalog)
            builder.UseSetting("ExternalServices:ProductCatalog:BaseUrl", _productCatalogBaseUrl!);

        builder.UseSetting("MongoDbSettings:ConnectionString", _connectionString);
        builder.UseSetting("MongoDbSettings:DatabaseName", _database);
        builder.UseSetting("MongoDbSettings:ShoppingCartsCollectionName", "shoppingCarts");
        builder.UseSetting("MongoDbSettings:OrdersCollectionName", "orders");
        builder.UseSetting("MongoDbSettings:ProductVersionsCollectionName", "productVersions");
        builder.UseSetting("MongoDbSettings:InvoicesCollectionName", "invoices");
        builder.UseSetting("MongoDbSettings:ClientDataVersionsCollectionName", "clientDataVersions");

        if (CatalogEvidenceHandlerFactory is not null)
        {
            builder.ConfigureTestServices(services => services.Configure<HttpClientFactoryOptions>(nameof(ProductApiClient), options =>
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                {
                    handlerBuilder.AdditionalHandlers.Add(CatalogEvidenceHandlerFactory());
                    handlerBuilder.AdditionalHandlers.Add(new CatalogRequestCounter(this));
                })));
        }

        if (_loseCompletionAckOnce)
        {
            builder.ConfigureTestServices(services =>
            {
                var original = services.Last(descriptor => descriptor.ServiceType == typeof(IInvoiceGenerationRepository));
                services.Remove(original);
                services.AddScoped<IInvoiceGenerationRepository>(provider =>
                    new LoseOnceInvoiceCompletionAcknowledgement(
                        (IInvoiceGenerationRepository)ActivatorUtilities.CreateInstance(provider, original.ImplementationType!),
                        _completionAckState));
            });
        }

        if (_failPdfOnce || _failDownload)
        {
            builder.ConfigureTestServices(services =>
            {
                var original = services.Last(descriptor => descriptor.ServiceType == typeof(IInvoicePdfService));
                services.Remove(original);
                services.AddScoped<IInvoicePdfService>(provider =>
                    new FailOnceInvoicePdfService(
                        (IInvoicePdfService)ActivatorUtilities.CreateInstance(provider, original.ImplementationType!),
                        _pdfFailureState, _failDownload));
            });
        }

        if (InvoiceProbe is not null)
        {
            builder.ConfigureTestServices(services =>
            {
                var repository = services.Last(descriptor => descriptor.ServiceType == typeof(IInvoiceGenerationRepository));
                services.Remove(repository);
                services.AddScoped<IInvoiceGenerationRepository>(provider => new ObservedInvoiceGenerationRepository(
                    (IInvoiceGenerationRepository)(repository.ImplementationFactory?.Invoke(provider)
                        ?? ActivatorUtilities.CreateInstance(provider, repository.ImplementationType!)), InvoiceProbe));
                var pdf = services.Last(descriptor => descriptor.ServiceType == typeof(IInvoicePdfService));
                services.Remove(pdf);
                services.AddScoped<IInvoicePdfService>(provider => new ObservedInvoicePdfService(
                    (IInvoicePdfService)(pdf.ImplementationFactory?.Invoke(provider)
                        ?? ActivatorUtilities.CreateInstance(provider, pdf.ImplementationType!)), InvoiceProbe));
            });
        }

        if (OrderProbe is not null)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpContextAccessor();
                services.AddScoped<OrderProbeScope>();
                Decorate<IOrderRepository>(services, (inner, provider) => new ObservedOrderRepository(inner, provider.GetRequiredService<MongoDbContext>(), OrderProbe, provider.GetRequiredService<OrderProbeScope>()));
                Decorate<IProductVersionRepository>(services, (inner, provider) => new ObservedProductVersionRepository(inner, provider.GetRequiredService<MongoDbContext>(), OrderProbe, provider.GetRequiredService<OrderProbeScope>()));
                Decorate<IShoppingCartRepository>(services, (inner, provider) => new ObservedShoppingCartRepository(inner, provider.GetRequiredService<MongoDbContext>(), OrderProbe, provider.GetRequiredService<OrderProbeScope>()));
                Decorate<IOrderWriteTransaction>(services, (inner, provider) => new ObservedOrderTransaction(inner, provider.GetRequiredService<MongoDbContext>(), OrderProbe, provider.GetRequiredService<OrderProbeScope>()));
            });
        }

        if (ReadProbe is not null)
        {
            builder.ConfigureTestServices(services =>
            {
                var pdf = services.Last(descriptor => descriptor.ServiceType == typeof(IInvoicePdfService));
                services.Remove(pdf);
                services.AddScoped<IInvoicePdfService>(provider => new ObservedInvoiceReadService(
                    (IInvoicePdfService)(pdf.ImplementationFactory?.Invoke(provider)
                        ?? ActivatorUtilities.CreateInstance(provider, pdf.ImplementationType!)), ReadProbe));
            });
        }

        ConfigureTechnicalServices(builder);

    }

    private static void Decorate<T>(IServiceCollection services, Func<T, IServiceProvider, T> decorate) where T : class
    {
        var original = services.Last(descriptor => descriptor.ServiceType == typeof(T));
        services.Remove(original);
        services.AddScoped<T>(provider => decorate((T)(original.ImplementationFactory?.Invoke(provider)
            ?? ActivatorUtilities.CreateInstance(provider, original.ImplementationType!)), provider));
    }

    internal async Task ExpireInvoiceLeaseAsync(Guid invoiceId)
    {
        var collection = new MongoClient(_connectionString).GetDatabase(_database).GetCollection<BsonDocument>("invoices");
        var result = await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(invoiceId, GuidRepresentation.Standard)),
            Builders<BsonDocument>.Update.Set("GenerationLeaseUntil", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        if (result.ModifiedCount != 1) throw new InvalidOperationException("Lease fixture did not change exactly one live claim.");
    }

    public async Task InitializeAsync()
    {
        await EnsurePlaywrightInstalledAsync();
        if (ReadStorage is not null) await ReadStorage.InitializeAsync();

        if (_useProductCatalog)
        {
            if (_isolatedProducts)
            {
                _ownedProducts = new ProductCatalogContainerFixture();
                await _ownedProducts.InitializeAsync();
            }
            _productCatalogBaseUrl = (_ownedProducts ?? await StartProducts.Value).BaseAddress.ToString();
        }

        if (!await StartTechnicalMongoAsync())
        {
            await StartMongo.Value;
            _connectionString = SharedMongoContainer.Value.GetConnectionString();
        }

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

    public Task<List<BsonDocument>> GetShoppingCartDocumentsAsync() =>
        new MongoClient(_connectionString).GetDatabase(_database)
            .GetCollection<BsonDocument>("shoppingCarts")
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

    public Task SeedShoppingCartDocumentAsync(BsonDocument document) =>
        new MongoClient(_connectionString).GetDatabase(_database)
            .GetCollection<BsonDocument>("shoppingCarts").InsertOneAsync(document);

    public async Task<Dictionary<string, BsonDocument[]>> GetCheckoutDocumentsAsync()
    {
        var database = new MongoClient(_connectionString).GetDatabase(_database);
        var snapshot = new Dictionary<string, BsonDocument[]>(StringComparer.Ordinal);
        foreach (var name in new[] { "shoppingCarts", "orders", "productVersions", "invoices", "clientDataVersions" })
        {
            var documents = await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            snapshot.Add(name, documents.OrderBy(document => document["_id"].AsBsonBinaryData.ToGuid()).ToArray());
        }
        return snapshot;
    }

    public async Task SeedOrderReadDocumentsAsync(Dictionary<string, BsonDocument[]> documents)
    {
        var names = new[] { "shoppingCarts", "orders", "productVersions", "invoices", "clientDataVersions" };
        if (!documents.Keys.Order().SequenceEqual(names.Order()))
            throw new InvalidOperationException("Order fixtures must explicitly supply all five collections.");
        var database = new MongoClient(_connectionString).GetDatabase(_database);
        foreach (var name in names)
            if (documents[name].Length != 0)
                await database.GetCollection<BsonDocument>(name).InsertManyAsync(documents[name]);
    }

    internal Task ChangeIsolatedCatalogProductAsync(Guid productId, string name, string brand, decimal amount, string currency) =>
        (_ownedProducts ?? throw new InvalidOperationException("Catalog mutation requires isolated Products containers."))
            .ChangeProductFixtureAsync(productId, name, brand, amount, currency);

    internal Task StopIsolatedCatalogAsync() =>
        (_ownedProducts ?? throw new InvalidOperationException("Stopping Products requires isolated containers."))
            .StopApiAsync();

    private sealed class CatalogRequestCounter(ApplicationFactory owner) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref owner._catalogRequests);
            return base.SendAsync(request, cancellationToken);
        }
    }

    public async Task SeedInvoiceLookupAsync(Guid invoiceId, Guid orderId, string state)
    {
        var collection = new MongoClient(_connectionString).GetDatabase(_database).GetCollection<BsonDocument>("invoices");
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(invoiceId, GuidRepresentation.Standard),
            ["OrderId"] = new BsonBinaryData(orderId, GuidRepresentation.Standard),
            ["ClientDataVersionId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["StorageUrl"] = "file:///invoices/lookup.pdf",
            ["CreatedAt"] = DateTime.UtcNow
        };
        if (state != "legacy") document["GenerationStatus"] = state;
        await collection.InsertOneAsync(document);
    }

    public async Task<List<BsonDocument>> GetInvoiceDocumentsAsync(Guid orderId)
    {
        var collection = new MongoClient(_connectionString).GetDatabase(_database)
            .GetCollection<BsonDocument>("invoices");
        var filter = Builders<BsonDocument>.Filter.Eq(
            "OrderId", new BsonBinaryData(orderId, GuidRepresentation.Standard));
        return await collection.Find(filter).ToListAsync();
    }

    public async Task SeedDownloadInvoiceAsync(Guid invoiceId, string state)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root.Parent is not null && !File.Exists(Path.Combine(root.FullName, "ECommerceStoreInvoice.slnx")))
            root = root.Parent;
        var directory = Path.Combine(root.FullName, "Invoices");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{invoiceId:N}-{Guid.NewGuid():N}.pdf");
        if (state != "missing-file")
        {
            await File.WriteAllTextAsync(path, "%PDF-1.7\nDemo invoice\n%%EOF");
            _downloadFiles.Add(path);
        }
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(invoiceId, GuidRepresentation.Standard),
            ["OrderId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["ClientDataVersionId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["StorageUrl"] = new Uri(path).AbsoluteUri,
            ["CreatedAt"] = DateTime.UtcNow,
            ["GenerationStatus"] = state == "missing-file" ? "Completed" : state
        };
        await new MongoClient(_connectionString).GetDatabase(_database)
            .GetCollection<BsonDocument>("invoices").InsertOneAsync(document);
    }

    public new async Task DisposeAsync()
    {
        try
        {
            base.Dispose();
            OrderProbe?.ResumeAll();
            InvoiceProbe?.CleanupFiles();
            foreach (var path in _downloadFiles) File.Delete(path);
            if (!string.IsNullOrEmpty(_connectionString) && (_technicalMongo is null || _technicalMongoRunning))
                await new MongoClient(_connectionString).DropDatabaseAsync(_database);
        }
        finally
        {
            try { if (ReadStorage is not null) await ReadStorage.DisposeAsync(); }
            finally
            {
                try { if (_ownedProducts is not null) await _ownedProducts.DisposeAsync(); }
                finally { if (_technicalMongo is not null) await _technicalMongo.DisposeAsync(); }
            }
        }
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

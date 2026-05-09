using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ProductVersionRepositoryBenchmarks
{
    private const string DatabaseName = "product-version-repository-benchmarks";
    private const string ProductVersionsCollectionName = "product-versions";

    private MongoDbContainer _mongoContainer = null!;
    private MongoDbContext _context = null!;
    private ProductVersionRepository _repository = null!;

    private readonly List<Guid> _existingIds = new();
    private readonly List<ProductVersion> _existingProductVersions = new();

    private int _readIterationCounter;

    [GlobalSetup]
    public async Task Setup()
    {
        _mongoContainer = new MongoDbBuilder("mongo:8.0")
            .WithUsername("admin")
            .WithPassword("admin123")
            .WithCreateParameterModifier(parameter =>
            {
                parameter.HostConfig.Tmpfs = new Dictionary<string, string>
                {
                    { "/data/db", "rw" }
                };
            })
            .Build();

        await _mongoContainer.StartAsync();

        var settings = new MongoDbSettings
        {
            ConnectionString = _mongoContainer.GetConnectionString(),
            DatabaseName = DatabaseName,
            ProductVersionsCollectionName = ProductVersionsCollectionName,
            ShoppingCartsCollectionName = "shopping-carts",
            OrdersCollectionName = "orders",
            InvoicesCollectionName = "invoices",
            ClientDataVersionsCollectionName = "client-data-versions"
        };

        _context = new MongoDbContext(Options.Create(settings));
        _repository = new ProductVersionRepository(_context);

        await SeedDataAsync();
    }

    private async Task SeedDataAsync()
    {
        await _context.ProductVersions.DeleteManyAsync(Builders<ProductVersionDocument>.Filter.Empty);

        _existingIds.Clear();
        _existingProductVersions.Clear();
        _readIterationCounter = 0;

        for (var i = 0; i < 200; i++)
        {
            var id = Guid.NewGuid();
            var document = ProductVersionDocumentBenchmarkDataFactory.Create(id);

            await _context.ProductVersions.InsertOneAsync(document);

            _existingIds.Add(id);
            _existingProductVersions.Add(ProductVersionMapping.MapToDomain(document));
        }
    }

    [Benchmark]
    public async Task<ProductVersion?> GetProductVersionById()
    {
        var index = _readIterationCounter % _existingIds.Count;
        var id = _existingIds[index];
        _readIterationCounter++;

        return await _repository.GetProductVersionById(id);
    }

    [Benchmark]
    public async Task<ProductVersion> CreateProductVersion()
    {
        var document = ProductVersionDocumentBenchmarkDataFactory.Create(Guid.NewGuid());
        var newProductVersion = ProductVersionMapping.MapToDomain(document);

        return await _repository.CreateProductVersion(newProductVersion);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        if (_mongoContainer is not null)
        {
            await _mongoContainer.DisposeAsync();
        }
    }
}
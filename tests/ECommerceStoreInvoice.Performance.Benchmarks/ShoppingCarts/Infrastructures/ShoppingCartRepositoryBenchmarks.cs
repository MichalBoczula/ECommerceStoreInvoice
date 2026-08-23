using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ShoppingCartRepositoryBenchmarks
{
    private const string DatabaseName = "shopping-cart-repository-benchmarks";
    private const string ShoppingCartsCollectionName = "shopping-carts";

    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private MongoDbContainer _mongoContainer = null!;
    private MongoDbContext _context = null!;
    private ShoppingCartRepository _repository = null!;

    private readonly List<Guid> _existingClientIds = new();
    private readonly List<ShoppingCart> _existingCarts = new();

    private int _readIterationCounter;
    private int _updateIterationCounter;

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
            ShoppingCartsCollectionName = ShoppingCartsCollectionName,
            OrdersCollectionName = "orders",
            ProductVersionsCollectionName = "product-versions",
            InvoicesCollectionName = "invoices",
            ClientDataVersionsCollectionName = "client-data-versions"
        };

        _context = new MongoDbContext(Options.Create(settings));
        _repository = new ShoppingCartRepository(_context);

        var indexKeys = Builders<ShoppingCartDocument>.IndexKeys.Ascending(x => x.ClientId);
        await _context.ShoppingCarts.Indexes.CreateOneAsync(new CreateIndexModel<ShoppingCartDocument>(indexKeys));

        await SeedDataAsync();
    }

    private async Task SeedDataAsync()
    {
        await _context.ShoppingCarts.DeleteManyAsync(Builders<ShoppingCartDocument>.Filter.Empty);

        _existingClientIds.Clear();
        _existingCarts.Clear();
        _readIterationCounter = 0;
        _updateIterationCounter = 0;

        for (var i = 0; i < 200; i++)
        {
            var clientId = Guid.NewGuid();
            var document = ShoppingCartDocumentBenchmarkDataFactory.CreateWithLines(LinesCount, clientId);

            await _context.ShoppingCarts.InsertOneAsync(document);

            _existingClientIds.Add(clientId);
            _existingCarts.Add(ShoppingCartMapping.MapToDomain(document));
        }
    }

    [Benchmark]
    public async Task<ShoppingCart?> GetShoppingCartByClientId()
    {
        var index = _readIterationCounter % _existingClientIds.Count;
        var clientId = _existingClientIds[index];
        _readIterationCounter++;

        return await _repository.GetShoppingCartByClientId(clientId);
    }

    [Benchmark]
    public async Task<ShoppingCart> CreateShoppingCart()
    {
        var document = ShoppingCartDocumentBenchmarkDataFactory.CreateWithLines(LinesCount, Guid.NewGuid());
        var newCart = ShoppingCartMapping.MapToDomain(document);

        return await _repository.CreateShoppingCart(newCart);
    }

    [Benchmark]
    public async Task<ShoppingCart> UpdateShoppingCart()
    {
        var index = _updateIterationCounter % _existingCarts.Count;
        var cart = _existingCarts[index];
        _updateIterationCounter++;

        return await _repository.UpdateShoppingCart(cart);
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
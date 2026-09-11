using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class OrderWithProductsMappingBenchmarks
{
    private const string DatabaseName = "order-with-products-benchmarks";

    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private OrderWithProductsDocument _document = null!;
    private MongoDbContainer _mongoContainer = null!;
    private OrderRepository _repository = null!;
    private readonly List<Guid> _existingOrderIds = [];
    private readonly List<Guid> _existingClientIds = [];
    private int _readByIdIterationCounter;
    private int _readByClientIterationCounter;

    [GlobalSetup]
    public async Task Setup()
    {
        _document = OrderWithProductsDocumentBenchmarkDataFactory.CreateWithLinesAndProductVersions(
            LinesCount,
            Guid.NewGuid(),
            Guid.NewGuid());

        _mongoContainer = new MongoDbBuilder("mongo:8.0")
            .WithUsername("admin")
            .WithPassword("admin123")
            .WithCreateParameterModifier(p => p.HostConfig.Tmpfs = new Dictionary<string, string> { { "/data/db", "rw" } })
            .Build();

        await _mongoContainer.StartAsync();

        var context = new MongoDbContext(Options.Create(new MongoDbSettings
        {
            ConnectionString = _mongoContainer.GetConnectionString(),
            DatabaseName = DatabaseName,
            OrdersCollectionName = "orders",
            ShoppingCartsCollectionName = "shopping-carts",
            ProductVersionsCollectionName = "product-versions",
            InvoicesCollectionName = "invoices",
            ClientDataVersionsCollectionName = "client-data-versions"
        }));

        _repository = new OrderRepository(context);

        await context.Orders.Indexes.CreateOneAsync(
            new CreateIndexModel<OrderDocument>(Builders<OrderDocument>.IndexKeys.Ascending(x => x.ClientId)));

        for (var i = 0; i < 200; i++)
        {
            var orderId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var benchmarkDocument = OrderWithProductsDocumentBenchmarkDataFactory.CreateWithLinesAndProductVersions(
                LinesCount,
                orderId,
                clientId);
            var orderDocument = new OrderDocument
            {
                Id = benchmarkDocument.Id,
                ClientId = benchmarkDocument.ClientId,
                CreatedAt = benchmarkDocument.CreatedAt,
                UpdatedAt = benchmarkDocument.UpdatedAt,
                Status = benchmarkDocument.Status,
                Lines = benchmarkDocument.Lines
            };

            await context.Orders.InsertOneAsync(orderDocument);
            await context.ProductVersions.InsertManyAsync(benchmarkDocument.ProductVersions);

            _existingOrderIds.Add(orderId);
            _existingClientIds.Add(clientId);
        }
    }

    [Benchmark]
    public (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions) MapDocumentToDomain()
    {
        return OrderMapping.MapToDomain(_document);
    }

    [Benchmark]
    public async Task<IReadOnlyCollection<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)>> GetOrdersWithProductVersionsByClientId()
    {
        var clientId = _existingClientIds[_readByClientIterationCounter % _existingClientIds.Count];
        _readByClientIterationCounter++;
        return await _repository.GetOrdersWithProductVersionsByClientId(clientId);
    }

    [Benchmark]
    public async Task<(Order Order, IReadOnlyCollection<ProductVersion> ProductVersions)?> GetOrderWithProductVersionsById()
    {
        var orderId = _existingOrderIds[_readByIdIterationCounter % _existingOrderIds.Count];
        _readByIdIterationCounter++;
        return await _repository.GetOrderWithProductVersionsById(orderId);
    }

    [GlobalCleanup]
    public async Task Cleanup() => await _mongoContainer.DisposeAsync();
}
